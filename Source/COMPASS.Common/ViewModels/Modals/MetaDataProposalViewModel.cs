using System;
using System.Collections.Generic;
using System.Linq;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Modals;

public class MetaDataProposalViewModel : ViewModelBase, IDisposable
{
    public MetaDataProposalViewModel(Codex codex, SourceMetaData proposedMetaData)
    {
        Codex = codex;
        ExistingMetaData = new(new(codex));
        ProposedMetaData = new(proposedMetaData);

        ShouldUseNewValue = ServiceResolver.Resolve<IPreferencesService>().Preferences.ImportableCodexProperties
                                              .ToDictionary(prop => prop.Name, _ => false);
    }
    
    public Codex Codex { get; set; }
    
    public SourceMetaDataViewModel ExistingMetaData { get; set; }
    public SourceMetaDataViewModel ProposedMetaData { get; set; }
    
    public Dictionary<string, bool> ShouldUseNewValue { get; }
    
    public void ApplyChoice()
    {
        var propsToApply = ServiceResolver.Resolve<IPreferencesService>().Preferences.ImportableCodexProperties
                                             .Where(prop => ShouldUseNewValue[prop.Name]);
        foreach (CodexProperty? prop in propsToApply)
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