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
        ExistingMetaData = new(codex);
        ProposedMetaData = proposedMetaData;
        
        _propsToAsk = PreferencesService.GetInstance().Preferences.ImportableCodexProperties
            .Where(prop => prop.OverwriteMode == MetaDataOverwriteMode.Ask)
            .ToList();
        
        foreach (var prop in _propsToAsk)
        {
            //Use the new value by default because it is probably more up to date
            ShouldUseNewValue.Add(prop.Name, true);
        }
    }

    private readonly List<CodexProperty> _propsToAsk;
    
    public Codex Codex { get; set; }
    
    public SourceMetaData ExistingMetaData { get; set; }
    public SourceMetaData ProposedMetaData { get; set; }
    
    public Dictionary<string, bool> ShouldUseNewValue { get; } = [];
    
    public void AppplyChoice()
    {
        foreach (var prop in _propsToAsk)
        {
            if (ShouldUseNewValue[prop.Name])
            {
                prop.Apply(ProposedMetaData, Codex);
            }
        }
    }

    public void Dispose()
    {
       ProposedMetaData.Cover?.Dispose();
    }
}