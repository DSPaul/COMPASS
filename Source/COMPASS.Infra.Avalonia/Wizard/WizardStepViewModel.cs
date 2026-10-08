using COMPASS.Infra.Avalonia.Validation;

namespace COMPASS.Infra.Avalonia.Wizard;

public class WizardStepViewModel : ValidatableViewModelBase
{
    public WizardStepViewModel(string title, string identifier)
    {
        Title = title;
        Identifier = identifier;
    }
    
    public WizardStepViewModel(string identifier) : this(identifier, identifier){}

    public string Title { get; }

    public string Identifier { get; }
}