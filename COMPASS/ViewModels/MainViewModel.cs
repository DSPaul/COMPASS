using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COMPASS.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel(string FolderName)
        {
            currentData = new Data(FolderName);
        }

        private Data currentData;

        public Data CurrentData
        {
            get { return currentData; }
            private set { SetProperty(ref currentData, value); }
        }
    }
}
