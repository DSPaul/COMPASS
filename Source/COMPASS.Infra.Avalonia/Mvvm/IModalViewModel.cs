namespace COMPASS.Infra.Avalonia.Mvvm;

public interface IModalViewModel
{
    /// <summary>
    /// The title of the modal window
    /// </summary>
    string WindowTitle { get; }

    /// <summary>
    /// Callback set by the modal that can be used to close the window from the vm
    /// </summary>
    Action CloseAction { get; set; }
}