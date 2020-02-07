using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class BaseEditViewModel : BaseViewModel
    {
        public BaseEditViewModel(MainViewModel vm)
        {
            MVM = vm;
            CancelCommand = new BasicCommand(Cancel);
            OKCommand = new BasicCommand(OKBtn);
        }

        //MainViewModel
        private MainViewModel mainViewModel;
        public MainViewModel MVM
        {
            get { return mainViewModel; }
            set { SetProperty(ref mainViewModel, value); }
        }

        public Action CloseAction { get; set; }

        public BasicCommand CancelCommand { get; private set; }
        public void Cancel()
        {
            CloseAction();
        }

        public BasicCommand OKCommand { get; private set; }
        public virtual void OKBtn() { }

    }
}
