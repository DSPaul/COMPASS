using System.Globalization;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Converters;

public class WizardStepTemplateSelector : IValueConverter
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = [];
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is WizardStepViewModel stepVM)
        {
            if (AvailableTemplates.TryGetValue(stepVM.Identifier, out var template))
            {
                return template;
            }
            throw new($"Template for wizard step {stepVM.Identifier} was not found");
        }
        
        throw new($"Object is not a {nameof(WizardStepViewModel)}");
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}