using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class SettingsViewModel
    {
        public SettingsViewModel(MainViewModel mvm)
        {
            MVM = mvm;
        }

        private MainViewModel MVM;

        #region Functions

        public void RegenAllThumbnails() { 
            foreach(Codex codex in MVM.CurrentCollection.AllFiles)
            {
                //codex.Thumbnail = codex.CoverArt.Replace("CoverArt", "Thumbnails");
                CoverArtGenerator.CreateThumbnail(codex);
            }
        }
        #endregion
    }
}
