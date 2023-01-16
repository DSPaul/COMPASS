using COMPASS.ViewModels.Commands;
using System;

namespace COMPASS.ViewModels
{
    // Interface for windows with OK and Cancel buttons
    // which are all windows that edit a codex or tag
    public interface IEditViewModel
    {
        public Action CloseAction { get; set; }

        public ActionCommand CancelCommand { get; }
        public void Cancel();

        public ActionCommand OKCommand { get; }
        public void OKBtn();
    }
}
