using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Models;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Modals
{
    public class ChooseMetadataViewModel : WizardViewModel
    {
        private readonly MetadataProposalViewModelFactory _metaDataProposalViewModelFactory;

        public ChooseMetadataViewModel(MetadataProposalViewModelFactory metaDataProposalViewModelFactory)
        {
            _metaDataProposalViewModelFactory = metaDataProposalViewModelFactory;
        }

        public List<MetadataProposalViewModel> MetadataProposals { get; } = [];
        
        public override string WindowTitle { get; } = "Choose which metadata to keep";
        
        private readonly Mutex _codicesListMutex = new();
        public void AddMetadataProposal(Codex codex, SourceMetadata proposedMetadata)
        {
            _codicesListMutex.WaitOne();
            if (MetadataProposals.AddIfMissing(_metaDataProposalViewModelFactory.Create(codex, proposedMetadata)))
            {
                Steps.Add(new WizardStepViewModel(codex.Title));
            }
            _codicesListMutex.ReleaseMutex();
        }

        public MetadataProposalViewModel CurrentProposal => MetadataProposals[StepCounter];

        protected override void NextStep()
        {
            StepCounter++;
            OnPropertyChanged(nameof(CurrentProposal));
        }

        protected override void PrevStep()
        {
            StepCounter--;
            OnPropertyChanged(nameof(CurrentProposal));
        }

        public override async Task Finish()
        {
            await ApplyChoices();
            CloseAction();
        }

        private async Task ApplyChoices()
        {
            foreach (var proposal in MetadataProposals)
            {
                await proposal.ApplyChoice();
                proposal.Dispose();
            }
            
            MetadataProposals.Clear();
        }
    }

    [Factory]
    public class ChooseMetadataViewModelFactory(MetadataProposalViewModelFactory metaDataProposalViewModelFactory)
    {
        public ChooseMetadataViewModel Create() => new(metaDataProposalViewModelFactory);
    }
}