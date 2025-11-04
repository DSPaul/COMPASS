using System;
using System.Collections.Generic;
using System.Linq;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;

namespace COMPASS.Common.ViewModels.Modals;

public class MetaDataProposalViewModel : ViewModelBase, IDisposable
{
    public MetaDataProposalViewModel(Codex codex, SourceMetaData proposedMetaData)
    {
        Codex = codex;
        ExistingMetaData = new(new(codex));
        ProposedMetaData = new(proposedMetaData);

        ShouldUseNewValue = PreferencesService.GetInstance().Preferences.ImportableCodexProperties
                                              .ToDictionary(prop => prop.Name, _ => false);
    }
    
    public Codex Codex { get; set; }
    
    public SourceMetaDataViewModel ExistingMetaData { get; set; }
    public SourceMetaDataViewModel ProposedMetaData { get; set; }
    
    public Dictionary<string, bool> ShouldUseNewValue { get; }
    
    public void ApplyChoice()
    {
        var propsToApply = PreferencesService.GetInstance().Preferences.ImportableCodexProperties
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