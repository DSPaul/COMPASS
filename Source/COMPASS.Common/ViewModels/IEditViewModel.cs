using CommunityToolkit.Mvvm.Input;
using System;

namespace COMPASS.Common.ViewModels
{
    /// <summary>
    /// Interface for windows with OK and Cancel buttons which are all windows that edit a codex or tag
    /// </summary>
    public interface IEditViewModel
    {
        public Action CloseAction { get; set; }

        public RelayCommand CancelCommand { get; }
        public void Cancel();

        public RelayCommand OKCommand { get; }
        public void OKBtn();
    }
}
