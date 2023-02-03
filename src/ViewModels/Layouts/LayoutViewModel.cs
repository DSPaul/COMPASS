using COMPASS.Models;
using COMPASS.Tools;

namespace COMPASS.ViewModels
{
    public abstract class LayoutViewModel : ViewModelBase
    {
        protected LayoutViewModel() : base() { }

        #region Properties

        public CodexViewModel CodexVM { get; init; } = new();

        //Selected File
        private Codex _selectedFile;
        public Codex SelectedFile
        {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }

        //Set Type of view
        public Enums.CodexLayout LayoutType { get; init; }
        #endregion
    }
}
