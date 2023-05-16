using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using System;
using System.Collections.Generic;
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
            for (var i = 0; i < CodicesWithChoices.Count; i++)
            {
                CodicesWithChoices[i].Item1.Copy(_codicesWithMadeChoices[i]);
            }
            CloseAction.Invoke();
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
                     ((IList<Tag>)prop.GetProp(_codicesWithMadeChoices[Counter])).Count > ((IList<Tag>)prop.GetProp(CurrentPair.Item1)).Count
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