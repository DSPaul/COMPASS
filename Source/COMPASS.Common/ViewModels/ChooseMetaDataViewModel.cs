using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using COMPASS.Common.Interfaces.Storage;

namespace COMPASS.Common.ViewModels
{
    public class ChooseMetaDataViewModel : WizardViewModel
    {
        public List<Tuple<Codex, Codex>> CodicesWithChoices { get; init; } = [];
        private List<Codex> _codicesWithMadeChoices = [];
        
        public override string WindowTitle { get; } = "Choose which metadata to keep";

        public List<CodexProperty> PropsToAsk =>
            PreferencesService.GetInstance().Preferences.CodexProperties
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

        public override ObservableCollection<WizardStepViewModel> Steps => 
            new(CodicesWithChoices.Select(pair => new WizardStepViewModel(pair.Item1.Title)));
        public Tuple<Codex, Codex> CurrentPair => CodicesWithChoices[StepCounter];

        protected override void NextStep()
        {
            ApplyChoice();
            StepCounter++;
            OnPropertyChanged(nameof(CurrentPair));
            //reset choices
            ShouldUseNewValue = DefaultShouldUseNewValue;
        }

        protected override void PrevStep()
        {
            ApplyChoice();
            StepCounter--;
            OnPropertyChanged(nameof(CurrentPair));
            //copy new codex to temp
            ShouldUseNewValue = DefaultShouldUseNewValue;
        }

        protected override Task Finish()
        {
            var thumbnailStorageService = App.Container.Resolve<IThumbnailStorageService>();
            
            ApplyChoice();
            CloseAction();

            for (var i = 0; i < CodicesWithChoices.Count; i++)
            {
                //cover art needs to copy the file, rather than path
                if (_codicesWithMadeChoices[i].CoverArtPath != CodicesWithChoices[i].Item1.CoverArtPath)
                {
                    File.Copy(_codicesWithMadeChoices[i].CoverArtPath, CodicesWithChoices[i].Item1.CoverArtPath, true);
                    CoverService.CreateThumbnail(CodicesWithChoices[i].Item1);
                    CodicesWithChoices[i].Item1.RefreshThumbnail();
                }
                //delete temp cover if it exists
                try
                {
                    if (CodicesWithChoices[i].Item2.CoverArtPath.EndsWith(".tmp.png"))
                        File.Delete(CodicesWithChoices[i].Item2.CoverArtPath);
                    if (CodicesWithChoices[i].Item2.ThumbnailPath.EndsWith(".tmp.png"))
                        File.Delete(CodicesWithChoices[i].Item2.ThumbnailPath);
                }
                catch (Exception ex)
                {
                    //no big deal if this fails
                    Logger.Warn("Failed to clean up temporary files", ex);
                }

                //Set image paths back so that copy operation after this doesn't change them to the temp files
                thumbnailStorageService.InitCodexImagePaths(_codicesWithMadeChoices[i]);

                //copy metadata data over
                CodicesWithChoices[i].Item1.CopyFrom(_codicesWithMadeChoices[i]);
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
                    bool useNew = prop.HasNewValue(_codicesWithMadeChoices[StepCounter], CurrentPair.Item1);
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