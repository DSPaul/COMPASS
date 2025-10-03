using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Modals;

namespace COMPASS.Common.ViewModels
{
    public class ChooseMetaDataViewModel : WizardViewModel
    {
        public List<MetaDataProposalViewModel> MetaDataProposals { get; } = [];
        
        public override string WindowTitle { get; } = "Choose which metadata to keep";
            

        private readonly Mutex _codicesListMutex = new();
        public void AddMetaDataProposal(Codex codex, SourceMetaData proposedMetaData)
        {
            _codicesListMutex.WaitOne();
            MetaDataProposals.AddIfMissing(new(codex, proposedMetaData));
            _codicesListMutex.ReleaseMutex();
        }

        public override ObservableCollection<WizardStepViewModel> Steps => 
            new(MetaDataProposals.Select(choice => new WizardStepViewModel(choice.Codex.Title)));
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

        protected override Task Finish()
        {
            ApplyChoices();
            CloseAction();
            return Task.CompletedTask;
        }

        private void ApplyChoices()
        {
            foreach (var proposal in MetaDataProposals)
            {
                proposal.AppplyChoice();
                proposal.Dispose();
            }
            
            MetaDataProposals.Clear();
        }
    }
}