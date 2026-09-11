using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Models;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Modals
{
    public class ChooseMetaDataViewModel : WizardViewModel
    {
        private readonly MetaDataProposalViewModelFactory _metaDataProposalViewModelFactory;

        public ChooseMetaDataViewModel(MetaDataProposalViewModelFactory metaDataProposalViewModelFactory)
        {
            _metaDataProposalViewModelFactory = metaDataProposalViewModelFactory;
        }

        public List<MetaDataProposalViewModel> MetaDataProposals { get; } = [];
        
        public override string WindowTitle { get; } = "Choose which metadata to keep";
        
        private readonly Mutex _codicesListMutex = new();
        public void AddMetaDataProposal(Codex codex, SourceMetaData proposedMetaData)
        {
            _codicesListMutex.WaitOne();
            if (MetaDataProposals.AddIfMissing(_metaDataProposalViewModelFactory.Create(codex, proposedMetaData)))
            {
                Steps.Add(new WizardStepViewModel(codex.Title));
            }
            _codicesListMutex.ReleaseMutex();
        }

        public MetaDataProposalViewModel CurrentProposal => MetaDataProposals[StepCounter];

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
            foreach (var proposal in MetaDataProposals)
            {
                await proposal.ApplyChoice();
                proposal.Dispose();
            }
            
            MetaDataProposals.Clear();
        }
    }

    [Factory]
    public class ChooseMetaDataViewModelFactory(MetaDataProposalViewModelFactory metaDataProposalViewModelFactory)
    {
        public ChooseMetaDataViewModel Create() => new(metaDataProposalViewModelFactory);
    }
}