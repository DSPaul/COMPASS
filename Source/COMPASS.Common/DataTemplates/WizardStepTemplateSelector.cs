using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Modals.Import;

namespace COMPASS.Common.DataTemplates;

//based on https://github.com/AvaloniaUI/Avalonia.Samples/tree/main/src/Avalonia.Samples/DataTemplates/IDataTemplateSample

public class WizardStepTemplateSelector : IDataTemplate
{
    // This Dictionary should store our templates. We mark this as [Content], so we can directly add elements to it later.
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = [];
    
    public Control? Build(object? param) => GetTemplate(param).Build(param);
    
    public bool Match(object? data) => data is WizardViewModel || data is WizardStepViewModel;
    
    private IDataTemplate GetTemplate(object? param)
    {
        if (param is WizardViewModel wizardViewModel)
        {
            return AvailableTemplates[wizardViewModel.CurrentStep.Identifier];
        }
        if (param is WizardStepViewModel wizardStepViewModel)
        {
            return AvailableTemplates[wizardStepViewModel.Identifier];
        }
        
        throw new ArgumentException($"Parameter {param} is not of type {typeof(WizardStepViewModel)}");
    }
}