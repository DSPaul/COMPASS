namespace COMPASS.Common.ViewModels;

public class WizardStepViewModel : ViewModelBase
{
    private string _title;
    private string _identifier;

    public WizardStepViewModel(string title, string identifier)
    {
        _title = title;
        _identifier = identifier;
    }
    
    public WizardStepViewModel(string identifier) : this(identifier, identifier){}

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Identifier
    {
        get => _identifier;
        set => SetProperty(ref _identifier, value);
    }
}