using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Infra.Avalonia.Modal;
using Material.Icons;

namespace COMPASS.Common.Features.Onboarding;

/// <summary>
/// A single page of the upgrade onboarding
/// </summary>
public record OnboardingStep(MaterialIconKind Icon, string Title, string Description);

/// <summary>
/// Shown once to users updating from v1 to v2, pointing out where things moved
/// </summary>
public class V1toV2OnboardingVM : ObservableObject, IModalViewModel
{
    public IReadOnlyList<OnboardingStep> Steps { get; } =
    [
        new(MaterialIconKind.PartyPopper,
            "Welcome to COMPASS 2.0",
            "COMPASS has been rebuilt from the ground up. It's faster, runs on Linux and almost every screen got a makeover. " +
            "Your data such as your collections, tags and settings came along with you. " +
            "Some features have moved, this quick tour shows you where to find what you're used to. "),
        new(MaterialIconKind.Tab,
            "Collections now live in tabs",
            "You can now have several collections open at once, each in its own tab with its own layout and filters. " +
            "The options to create or rename a collection have been moved to the the More Actions (⋮) menu. "),
        new(MaterialIconKind.Sort,
            "Sorting and layout customization has moved",
            "They used to be sidepanels on the left. They are now buttons next to the layout selection in the top right, which has also become a dropdown. "),
        new(MaterialIconKind.Link,
            "Folder - Tag linking has moved",
            "It has always been possible to link a folder on your PC to a tag. This will assign the tag to all files imported from that folder. " +
            "However this feature was hidden in the settings, it is now accessible in the tag Edit dialog. "),
        new(MaterialIconKind.Settings,
            "The settings window has also gotten a major overhaul",
            "The settings have been redesigned to have better visual consistency. The grouping of settings has also changed to be more intuitive. " +
            "Some settings have also been added and removed."),
    ];

    private int _currentStepIndex;
    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set
        {
            if (SetProperty(ref _currentStepIndex, value))
            {
                OnPropertyChanged(nameof(CurrentStep));
                OnPropertyChanged(nameof(IsLastStep));
                OnPropertyChanged(nameof(StepCounterText));
                PreviousCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public OnboardingStep CurrentStep => Steps[CurrentStepIndex];
    public bool IsLastStep => CurrentStepIndex == Steps.Count - 1;
    public string StepCounterText => $"{CurrentStepIndex + 1} of {Steps.Count}";

    private RelayCommand? _previousCommand;
    public RelayCommand PreviousCommand => _previousCommand ??= new(() => CurrentStepIndex--, () => CurrentStepIndex > 0);

    private RelayCommand? _nextCommand;
    public RelayCommand NextCommand => _nextCommand ??= new(Next);
    private void Next()
    {
        if (IsLastStep)
        {
            CloseAction();
            return;
        }
        CurrentStepIndex++;
    }

    private RelayCommand? _skipCommand;
    public RelayCommand SkipCommand => _skipCommand ??= new(() => CloseAction());

    #region IModalViewModel

    public string WindowTitle => "What changed in COMPASS 2.0";
    public Action CloseAction { get; set; } = () => { };

    #endregion
}
