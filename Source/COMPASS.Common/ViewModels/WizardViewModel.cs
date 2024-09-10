using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels
{
    public abstract class WizardViewModel : ViewModelBase
    {
        protected WizardViewModel()
        {
            Steps.CollectionChanged += (_, _) => OnStepsChanged();
        }

        private ObservableCollection<string> _steps = new();
        public virtual ObservableCollection<string> Steps
        {
            get => _steps;
            set
            {
                SetProperty(ref _steps, value);
                _steps.CollectionChanged += (_, _) => OnStepsChanged();
                OnStepsChanged();
            }
        }

        private int _stepCounter = 0;
        public int StepCounter
        {
            get => _stepCounter;
            set
            {
                if (value <= 0)
                {
                    value = 0;
                }
                else if (value >= Steps.Count)
                {
                    Finish();
                }

                SetProperty(ref _stepCounter, value);
                OnStepsChanged();
            }
        }

        protected void OnStepsChanged()
        {
            OnPropertyChanged(nameof(CurrentStep));
            RefreshNavigationBtns();
        }

        protected void RefreshNavigationBtns()
        {
            PrevStepCommand.NotifyCanExecuteChanged();
            NextStepCommand.NotifyCanExecuteChanged();
            FinishCommand.NotifyCanExecuteChanged();
        }

        public string CurrentStep => StepCounter >= Steps.Count ? "" : Steps[StepCounter];

        private RelayCommand? _cancelCommand;
        public virtual RelayCommand CancelCommand => _cancelCommand ??= new((CancelAction ?? CloseAction) ?? new Action(() => { }));

        private RelayCommand? _nextStepCommand;
        public RelayCommand NextStepCommand => _nextStepCommand ??= new(NextStep, ShowNextButton);
        protected virtual void NextStep() => StepCounter++;
        public virtual bool ShowNextButton() => StepCounter < Steps.Count - 1;

        private RelayCommand? _prevStepCommand;
        public RelayCommand PrevStepCommand => _prevStepCommand ??= new(PrevStep, ShowBackButton);
        protected virtual void PrevStep() => StepCounter--;
        public virtual bool ShowBackButton() => StepCounter > 0;

        private AsyncRelayCommand? _finishCommand;
        public AsyncRelayCommand FinishCommand => _finishCommand ??= new(Finish, ShowFinishButton);
        public virtual Task Finish()
        {
            return Task.CompletedTask;
        }
        public virtual bool ShowFinishButton() => StepCounter == Steps.Count - 1;

        public Action? CloseAction;
        public Action? CancelAction;
    }
}