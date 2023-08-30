using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace COMPASS.ViewModels
{
    public class ChooseMetaDataViewModel : ObservableObject
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

        public Tuple<Codex, Codex> CurrentPair => CodicesWithChoices[Counter];

        private int _counter = 0;
        public int Counter
        {
            get => _counter;
            private set
            {
                SetProperty(ref _counter, value);
                RaisePropertyChanged(nameof(CurrentPair));
                RaisePropertyChanged(nameof(ShowBackButton));
                RaisePropertyChanged(nameof(ShowNextButton));
                RaisePropertyChanged(nameof(ShowFinishButton));
            }
        }


        private ActionCommand _nextCommand;
        public ActionCommand NextCommand => _nextCommand ??= new(Next);
        private void Next()
        {
            //Copy pref into codex
            ApplyChoice();
            //go to the next one
            if (Counter + 1 == CodicesWithChoices.Count) return;
            Counter++;
            //reset choices
            ShouldUseNewValue = DefaultShouldUseNewValue;
        }

        private ActionCommand _backCommand;
        public ActionCommand BackCommand => _backCommand ??= new(Back);
        public void Back()
        {
            if (Counter == 0) return;
            //Copy pref into codex
            ApplyChoice();
            Counter--;
            //copy new codex to temp
            ShouldUseNewValue = DefaultShouldUseNewValue;
        }

        private ActionCommand _finishCommand;
        public ActionCommand FinishCommand => _finishCommand ??= new(Finish);
        private void Finish()
        {
            ApplyChoice();
            CloseAction.Invoke();

            for (var i = 0; i < CodicesWithChoices.Count; i++)
            {
                //cover art needs to copy the file, rather than path
                if (_codicesWithMadeChoices[i].CoverArt != CodicesWithChoices[i].Item1.CoverArt)
                {
                    File.Copy(_codicesWithMadeChoices[i].CoverArt, CodicesWithChoices[i].Item1.CoverArt, true);
                    CoverFetcher.CreateThumbnail(CodicesWithChoices[i].Item1);
                    CodicesWithChoices[i].Item1.RefreshThumbnail();
                }
                //delete temp cover if it exists
                if (CodicesWithChoices[i].Item2.CoverArt?.EndsWith(".tmp.png") == true)
                    File.Delete(CodicesWithChoices[i].Item2.CoverArt);
                if (CodicesWithChoices[i].Item2.Thumbnail?.EndsWith(".tmp.png") == true)
                    File.Delete(CodicesWithChoices[i].Item2.Thumbnail);

                //Set image paths back so that copy operataion after this doesn't change them to the temp files
                _codicesWithMadeChoices[i].SetImagePaths(MainViewModel.CollectionVM.CurrentCollection);

                //copy metadata data over
                CodicesWithChoices[i].Item1.Copy(_codicesWithMadeChoices[i]);
            }
        }

        private void ApplyChoice()
        {
            foreach (var prop in PropsToAsk)
            {
                if (ShouldUseNewValue[prop.Label])
                {
                    prop.SetProp(_codicesWithMadeChoices[Counter], CurrentPair.Item2);
                }
                else
                {
                    prop.SetProp(_codicesWithMadeChoices[Counter], CurrentPair.Item1);
                    if (prop.Label == "Tags")
                    {
                        //Because Tags setProp adds instead of overwrites, have to do it different
                        _codicesWithMadeChoices[Counter].Tags = new(CurrentPair.Item1.Tags);
                    }
                }
            }
        }

        public Action CloseAction { get; set; }

        public bool ShowBackButton => Counter > 0;
        public bool ShowNextButton => Counter < CodicesWithChoices.Count - 1;
        public bool ShowFinishButton => Counter == CodicesWithChoices.Count - 1;

        private Dictionary<string, bool> _shouldUseNewValue;
        private Dictionary<string, bool> DefaultShouldUseNewValue
        {
            get
            {
                Dictionary<string, bool> dict = new();
                foreach (var prop in PropsToAsk)
                {
                    bool useNew = prop.Label == "Tags" ?
                     //for tags, new value was chosesn when there are more tags in the list
                     ((IList<Tag>)prop.GetProp(_codicesWithMadeChoices[Counter])).Count > ((IList<Tag>)prop.GetProp(CurrentPair.Item1)).Count
                     // for all the other, do a string compare to see if the new options was chosen
                     : prop.GetProp(CurrentPair.Item1)?.ToString() != prop.GetProp(_codicesWithMadeChoices[Counter])?.ToString();

                    dict.Add(prop.Label, useNew);
                }
                return dict;
            }
        }

        public Dictionary<string, bool> ShouldUseNewValue
        {
            get => _shouldUseNewValue ??= DefaultShouldUseNewValue;
            set => SetProperty(ref _shouldUseNewValue, value);
        }
    }
}