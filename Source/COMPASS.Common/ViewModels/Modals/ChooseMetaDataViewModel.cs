using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;

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
            MetaDataProposals.AddIfMissing(new(codex, proposedMetaData));
            _codicesListMutex.ReleaseMutex();
        }

        //TODO do not create a new collection on every get
        public override RangeObservableCollection<WizardStepViewModel> Steps => 
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