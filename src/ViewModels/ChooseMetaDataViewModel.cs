using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class ChooseMetaDataViewModel : WizardViewModel
    {
        public List<Tuple<Codex, Codex>> CodicesWithChoices { get; init; } = new();
        private List<Codex> _codicesWithMadeChoices = new();

        public List<CodexProperty> PropsToAsk =>
            SettingsViewModel.GetInstance().MetaDataPreferences
            .Where(prop => prop.OverwriteMode == MetaDataOverwriteMode.Ask)
            .ToList();

        private readonly Mutex _codicesListMutex = new();
        public void AddCodexPair(Codex currentCodex, Codex proposedCodex)
        {
            _codicesListMutex.WaitOne();
            CodicesWithChoices.AddIfMissing(new(currentCodex, proposedCodex));
            _codicesWithMadeChoices.AddIfMissing(new(currentCodex));
            _codicesListMutex.ReleaseMutex();
        }

        public override ObservableCollection<string> Steps => new(CodicesWithChoices.Select(pair => pair.Item1.Title));
        public Tuple<Codex, Codex> CurrentPair => CodicesWithChoices[StepCounter];

        protected override void NextStep()
        {
            ApplyChoice();
            StepCounter++;
            RaisePropertyChanged(nameof(CurrentPair));
            //reset choices
            ShouldUseNewValue = DefaultShouldUseNewValue;
        }

        protected override void PrevStep()
        {
            ApplyChoice();
            StepCounter--;
            RaisePropertyChanged(nameof(CurrentPair));
            //copy new codex to temp
            ShouldUseNewValue = DefaultShouldUseNewValue;
        }

        public override Task Finish()
        {
            ApplyChoice();
            CloseAction?.Invoke();

            for (var i = 0; i < CodicesWithChoices.Count; i++)
            {
                //cover art needs to copy the file, rather than path
                if (_codicesWithMadeChoices[i].CoverArt != CodicesWithChoices[i].Item1.CoverArt)
                {
                    File.Copy(_codicesWithMadeChoices[i].CoverArt, CodicesWithChoices[i].Item1.CoverArt, true);
                    CoverService.CreateThumbnail(CodicesWithChoices[i].Item1);
                    CodicesWithChoices[i].Item1.RefreshThumbnail();
                }
                //delete temp cover if it exists
                if (CodicesWithChoices[i].Item2.CoverArt?.EndsWith(".tmp.png") == true)
                    File.Delete(CodicesWithChoices[i].Item2.CoverArt);
                if (CodicesWithChoices[i].Item2.Thumbnail?.EndsWith(".tmp.png") == true)
                    File.Delete(CodicesWithChoices[i].Item2.Thumbnail);

                //Set image paths back so that copy operation after this doesn't change them to the temp files
                _codicesWithMadeChoices[i].SetImagePaths(MainViewModel.CollectionVM.CurrentCollection);

                //copy metadata data over
                CodicesWithChoices[i].Item1.Copy(_codicesWithMadeChoices[i]);
            }
            return Task.CompletedTask;
        }

        private void ApplyChoice()
        {
            foreach (var prop in PropsToAsk)
            {
                if (ShouldUseNewValue[prop.Name])
                {
                    prop.SetProp(_codicesWithMadeChoices[StepCounter], CurrentPair.Item2);
                }
                else
                {
                    prop.SetProp(_codicesWithMadeChoices[StepCounter], CurrentPair.Item1);
                    if (prop.Name == nameof(Codex.Tags))
                    {
                        //Because Tags setProp adds instead of overwrites, have to do it different
                        _codicesWithMadeChoices[StepCounter].Tags = new(CurrentPair.Item1.Tags);
                    }
                }
            }
        }

        private Dictionary<string, bool> DefaultShouldUseNewValue
        {
            get
            {
                Dictionary<string, bool> dict = new();
                foreach (var prop in PropsToAsk)
                {
                    bool useNew = prop.Name == nameof(Codex.Tags) ?
                     //for tags, new value was chosen when there are more tags in the list
                     ((IList<Tag>)prop.GetProp(_codicesWithMadeChoices[StepCounter])!).Count > ((IList<Tag>)prop.GetProp(CurrentPair.Item1)!).Count
                     // for all the other, do a string compare to see if the new options was chosen
                     : prop.GetProp(CurrentPair.Item1)?.ToString() != prop.GetProp(_codicesWithMadeChoices[StepCounter])?.ToString();

                    dict.Add(prop.Name, useNew);
                }
                return dict;
            }
        }

        private Dictionary<string, bool>? _shouldUseNewValue;
        public Dictionary<string, bool> ShouldUseNewValue
        {
            get => _shouldUseNewValue ??= DefaultShouldUseNewValue;
            set => SetProperty(ref _shouldUseNewValue, value);
        }
    }
}