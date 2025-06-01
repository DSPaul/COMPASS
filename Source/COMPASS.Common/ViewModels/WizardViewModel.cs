using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.ViewModels;

namespace COMPASS.Common.ViewModels
{
    public abstract class WizardViewModel : ViewModelBase, IModalViewModel
    {
        protected WizardViewModel()
        {
            Steps.CollectionChanged += StepChangeHandler;
        }
        
        public virtual ObservableCollection<WizardStepViewModel> Steps { get; } = [];

        private int _stepCounter = 0;
        public int StepCounter
        {
            get => _stepCounter;
            protected set
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

        private void StepChangeHandler(object? sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            OnStepsChanged();
        }

        protected void RefreshNavigationBtns()
        {
            PrevStepCommand.NotifyCanExecuteChanged();
            NextStepCommand.NotifyCanExecuteChanged();
            FinishCommand.NotifyCanExecuteChanged();
        }

        public WizardStepViewModel CurrentStep => StepCounter >= Steps.Count ? Steps.Last() : Steps[StepCounter];

        private RelayCommand? _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??= new(CancelAction ?? CloseAction);
        protected virtual Action? CancelAction { get; } = null;

        private RelayCommand? _nextStepCommand;
        public RelayCommand NextStepCommand => _nextStepCommand ??= new(NextStep, ShowNextButton);
        protected virtual void NextStep() => StepCounter++;
        protected virtual bool ShowNextButton() => StepCounter < Steps.Count - 1;

        private RelayCommand? _prevStepCommand;
        public RelayCommand PrevStepCommand => _prevStepCommand ??= new(PrevStep, ShowBackButton);
        protected virtual void PrevStep() => StepCounter--;
        protected virtual bool ShowBackButton() => StepCounter > 0;

        private AsyncRelayCommand? _finishCommand;
        public AsyncRelayCommand FinishCommand => _finishCommand ??= new(Finish, ShowFinishButton);
        protected virtual Task Finish()
        {
            CloseAction();
            return Task.CompletedTask;
        }
        protected virtual bool ShowFinishButton() => StepCounter == Steps.Count - 1;
        
        public abstract string WindowTitle { get; }
        public Action CloseAction { get; set; } = () => { };
    }
}