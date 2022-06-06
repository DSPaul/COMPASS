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
            CancelCommand = new BasicCommand(Cancel);
            OKCommand = new BasicCommand(OKBtn);
        }

        private CodexCollection cc;
        public CodexCollection CurrentCollection
        {
            get { return cc; }
            set { SetProperty(ref cc, value); }
        }

        public Action CloseAction { get; set; }

        public BasicCommand CancelCommand { get; private set; }
        public virtual void Cancel(){}

        public BasicCommand OKCommand { get; private set; }
        public virtual void OKBtn() { }

    }
}
