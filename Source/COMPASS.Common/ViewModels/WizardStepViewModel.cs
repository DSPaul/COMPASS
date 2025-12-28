namespace COMPASS.Common.ViewModels;

public class WizardStepViewModel : ViewModelBase
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