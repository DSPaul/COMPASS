using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Modals;

public class MetaDataProposalViewModel : ViewModelBase, IDisposable
{
    private readonly Guid _proposalIdentifier = Guid.NewGuid();

    public MetaDataProposalViewModel(Codex codex, SourceMetaData proposedMetaData)
    {
        Codex = codex;
        ExistingMetaData = new(new(codex));
        ProposedMetaData = new(proposedMetaData);

        ShouldUseNewValue = ServiceResolver.Resolve<IPreferencesService>().Preferences.ImportableCodexProperties
                                              .ToDictionary(prop => prop.Name, prop => new ObservableKeyValuePair<CodexProperty, bool>(prop, false));
        MetaDataChoiceGroupNames = ShouldUseNewValue.Keys.ToDictionary(
            propertyName => propertyName,
            propertyName => $"{_proposalIdentifier:N}-{propertyName}");
    }
    
    public Codex Codex { get; set; }
    
    public SourceMetaDataViewModel ExistingMetaData { get; set; }
    public SourceMetaDataViewModel ProposedMetaData { get; set; }
    
    public Dictionary<string, ObservableKeyValuePair<CodexProperty, bool>> ShouldUseNewValue { get; }
    public Dictionary<string, string> MetaDataChoiceGroupNames { get; }
    
    public void ApplyChoice()
    {
        var propsToApply = ShouldUseNewValue.Values.Where(kvp => kvp.Value).Select(val => val.Key);
        foreach (CodexProperty prop in propsToApply)
        {
            prop.Apply(ProposedMetaData.GetSource(), Codex);
        }
    }

    public void Dispose()
    {
        ExistingMetaData.DeepDispose();
        ProposedMetaData.DeepDispose();
    }
}