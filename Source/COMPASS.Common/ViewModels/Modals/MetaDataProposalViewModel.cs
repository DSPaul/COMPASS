using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Infra.Models;

namespace COMPASS.Common.ViewModels.Modals;

public class MetadataProposalViewModel : ViewModelBase, IDisposable
{
    private readonly Guid _proposalIdentifier = Guid.NewGuid();
    private readonly ICoverService _coverService;

    public MetadataProposalViewModel(
        IPreferencesService preferencesService, ICoverService coverService, 
        Codex codex, SourceMetadata proposedMetadata)
    {
        Codex = codex;
        ExistingMetadata = new(new(codex));
        ProposedMetadata = new(proposedMetadata);

        _coverService = coverService;

        ShouldUseNewValue = preferencesService.Preferences.ImportableCodexProperties
                                              .ToDictionary(prop => prop.Name, prop => new ObservableKeyValuePair<CodexProperty, bool>(prop, false));
        MetadataChoiceGroupNames = ShouldUseNewValue.Keys.ToDictionary(
            propertyName => propertyName,
            propertyName => $"{_proposalIdentifier:N}-{propertyName}");
    }
    
    public Codex Codex { get; set; }
    
    public SourceMetadataViewModel ExistingMetadata { get; set; }
    public SourceMetadataViewModel ProposedMetadata { get; set; }
    
    public Dictionary<string, ObservableKeyValuePair<CodexProperty, bool>> ShouldUseNewValue { get; }
    public Dictionary<string, string> MetadataChoiceGroupNames { get; }
    
    public async Task ApplyChoice()
    {
        var propsToApply = ShouldUseNewValue.Values.Where(kvp => kvp.Value).Select(val => val.Key);
        var sourceMetadata = ProposedMetadata.GetSourceMetadata();
        foreach (CodexProperty prop in propsToApply)
        {
            if(prop is CoverProperty coverProp)
            {
                // Special handling for CoverProperty
                await coverProp.ApplyAsync(sourceMetadata, Codex, _coverService);
            }
            else
            {
                prop.Apply(sourceMetadata, Codex);
            }
        }
    }

    public void Dispose()
    {
        ExistingMetadata.DeepDispose();
        ProposedMetadata.DeepDispose();
    }
}

[Factory]
public class MetadataProposalViewModelFactory(IPreferencesService preferencesService, Lazy<ICoverService> coverService)
{
    //Lazy breaks the CoverService -> ChooseMetadataViewModelFactory ->
    //MetadataProposalViewModelFactory -> CoverService cycle: the service is only
    //resolved in Create, long after the container is built
    public MetadataProposalViewModel Create(Codex codex, SourceMetadata proposedMetadata) => new(preferencesService, coverService.Value, codex, proposedMetadata);
}
