using COMPASS.Commands;
using COMPASS.Models;
using System;
using System.Collections.ObjectModel;

namespace COMPASS.ViewModels
{
    public abstract class WizardViewModel : ObservableObject
    {
        private ObservableCollection<string> _steps = new();
        public virtual ObservableCollection<string> Steps
        {
            get => _steps;
            set
            {
                SetProperty(ref _steps, value);
                RaisePropertyChanged(nameof(CurrentStep));
            }
        }

        private int _stepCounter = 0;
        public int StepCounter
        {
            get => _stepCounter;
            set
            {
                if (value <= 0) value = 0;
                else if (value >= Steps.Count) ApplyAll();
                SetProperty(ref _stepCounter, value);
                RaisePropertyChanged(nameof(CurrentStep));
                RaisePropertyChanged(nameof(ShowBackButton));
                RaisePropertyChanged(nameof(ShowNextButton));
                RaisePropertyChanged(nameof(ShowFinishButton));
            }
        }

        public string CurrentStep => Steps[StepCounter];

        private ActionCommand _nextStepCommand;
        public ActionCommand NextStepCommand => _nextStepCommand ??= new(NextStep, ShowNextButton);
        protected virtual void NextStep() => StepCounter++;
        public virtual bool ShowNextButton() => StepCounter < Steps.Count - 1;

        private ActionCommand _prevStepCommand;
        public ActionCommand PrevStepCommand => _prevStepCommand ??= new(PrevStep, ShowBackButton);
        protected virtual void PrevStep() => StepCounter--;
        public virtual bool ShowBackButton() => StepCounter > 0;

        private ActionCommand _finishCommand;
        public ActionCommand FinishCommand => _finishCommand ??= new(ApplyAll, ShowFinishButton);
        public abstract void ApplyAll();
        public virtual bool ShowFinishButton() => StepCounter == Steps.Count - 1;

        public Action CloseAction;
    }
}
