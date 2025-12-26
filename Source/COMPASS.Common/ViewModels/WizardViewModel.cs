using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        public int StepCounter
        {
            get;
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

                SetProperty(ref field, value);
                OnStepsChanged();
            }
        } = 0;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName != nameof(HasErrors))
            {
                Validate(e.PropertyName);
            }
        }
        
        protected void OnStepsChanged()
        {
            //Steps can temporarily be empty because the observable collection cannot be reassigned, so we clear and refill instead
            //don't update the UI yet if that happens, we will update when the first item to be added back in
            if (Steps.Count > 0)
            {
                OnPropertyChanged(nameof(CurrentStep));
                RefreshNavigationBtns();
            }
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

        public RelayCommand CancelCommand => field ??= new(CancelAction ?? CloseAction);
        protected virtual Action? CancelAction { get; } = null;

        public RelayCommand NextStepCommand => field ??= new(NextStep, ShowNextButton);
        protected virtual void NextStep() => StepCounter++;
        protected virtual bool ShowNextButton() => !CurrentStep.HasErrors && StepCounter < Steps.Count - 1;

        public RelayCommand PrevStepCommand => field ??= new(PrevStep, ShowBackButton);
        protected virtual void PrevStep() => StepCounter--;
        protected virtual bool ShowBackButton() => StepCounter > 0;

        public AsyncRelayCommand FinishCommand => field ??= new(Finish, ShowFinishButton);
        public virtual Task Finish()
        {
            CloseAction();
            return Task.CompletedTask;
        }
        protected virtual bool ShowFinishButton() => !CurrentStep.HasErrors && StepCounter == Steps.Count - 1;

        protected override void OnValidated(string? propertyName)
        {
            base.OnValidated(propertyName);
            RefreshNavigationBtns();
        }

        public abstract string WindowTitle { get; }
        public Action CloseAction { get; set; } = () => { };
    }
}