using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class BaseEditViewModel : DealsWithTreeviews
    {
        public BaseEditViewModel() : base()
        {
            CurrentCollection = MVM.CurrentCollection;
            CancelCommand = new ActionCommand(Cancel);
            OKCommand = new ActionCommand(OKBtn);
        }

        private CodexCollection cc;
        public CodexCollection CurrentCollection
        {
            get { return cc; }
            set { SetProperty(ref cc, value); }
        }

        public Action CloseAction { get; set; }

        public ActionCommand CancelCommand { get; private set; }
        public virtual void Cancel(){}

        public ActionCommand OKCommand { get; private set; }
        public virtual void OKBtn() { }

    }
}
