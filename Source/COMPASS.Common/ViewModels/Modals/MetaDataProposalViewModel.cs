using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Infra.Models;

namespace COMPASS.Common.ViewModels.Modals;

public class MetaDataProposalViewModel : ViewModelBase, IDisposable
{
    private readonly Guid _proposalIdentifier = Guid.NewGuid();
    private readonly ICoverService _coverService;

    public MetaDataProposalViewModel(
        IPreferencesService preferencesService, ICoverService coverService, 
        Codex codex, SourceMetaData proposedMetaData)
    {
        Codex = codex;
        ExistingMetaData = new(new(codex));
        ProposedMetaData = new(proposedMetaData);

        _coverService = coverService;

        ShouldUseNewValue = preferencesService.Preferences.ImportableCodexProperties
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
    
    public async Task ApplyChoice()
    {
        var propsToApply = ShouldUseNewValue.Values.Where(kvp => kvp.Value).Select(val => val.Key);
        var sourceMetadata = ProposedMetaData.GetSourceMetaData();
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
        ExistingMetaData.DeepDispose();
        ProposedMetaData.DeepDispose();
    }
}

[Factory]
public class MetaDataProposalViewModelFactory(IPreferencesService preferencesService, Lazy<ICoverService> coverService)
{
    //Lazy breaks the CoverService -> ChooseMetaDataViewModelFactory ->
    //MetaDataProposalViewModelFactory -> CoverService cycle: the service is only
    //resolved in Create, long after the container is built
    public MetaDataProposalViewModel Create(Codex codex, SourceMetaData proposedMetaData) => new(preferencesService, coverService.Value, codex, proposedMetaData);
}
