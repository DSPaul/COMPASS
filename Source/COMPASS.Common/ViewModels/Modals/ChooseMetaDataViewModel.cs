using COMPASS.Common.Models;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Modals
{
    public class ChooseMetaDataViewModel : WizardViewModel
    {
        public List<MetaDataProposalViewModel> MetaDataProposals { get; } = [];
        
        public override string WindowTitle { get; } = "Choose which metadata to keep";
        
        private readonly Mutex _codicesListMutex = new();
        public void AddMetaDataProposal(Codex codex, SourceMetaData proposedMetaData)
        {
            _codicesListMutex.WaitOne();
            if (MetaDataProposals.AddIfMissing(new(codex, proposedMetaData)))
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

        public override Task Finish()
        {
            ApplyChoices();
            CloseAction();
            return Task.CompletedTask;
        }

        private void ApplyChoices()
        {
            foreach (var proposal in MetaDataProposals)
            {
                proposal.ApplyChoice();
                proposal.Dispose();
            }
            
            MetaDataProposals.Clear();
        }
    }
}