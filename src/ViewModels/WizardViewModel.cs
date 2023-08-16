using COMPASS.Commands;
using COMPASS.Models;
using System;
using System.Collections.ObjectModel;

namespace COMPASS.ViewModels
{
    public abstract class WizardViewModel : ObservableObject
    {
        private ObservableCollection<string> _steps = new();
        public ObservableCollection<string> Steps
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
                else if (value >= Steps.Count) Finish();
                SetProperty(ref _stepCounter, value);
                RaisePropertyChanged(nameof(CurrentStep));
                RaisePropertyChanged(nameof(ShowBackButton));
                RaisePropertyChanged(nameof(ShowNextButton));
                RaisePropertyChanged(nameof(ShowFinishButton));
            }
        }

        public string CurrentStep => Steps[StepCounter];

        private ActionCommand _nextStepCommand;
        public ActionCommand NextStepCommand => _nextStepCommand ??= new(NextStep);
        public virtual void NextStep() => StepCounter++;

        private ActionCommand _prevStepCommand;
        public ActionCommand PrevStepCommand => _prevStepCommand ??= new(PrevStep);
        public virtual void PrevStep() => StepCounter--;

        private ActionCommand _finishCommand;
        public ActionCommand FinishCommand => _finishCommand ??= new(Finish);
        public abstract void Finish();

        public bool ShowBackButton => StepCounter > 0;
        public bool ShowNextButton => StepCounter < Steps.Count - 1;
        public bool ShowFinishButton => StepCounter == Steps.Count - 1;

        public Action CloseAction;
    }
}
