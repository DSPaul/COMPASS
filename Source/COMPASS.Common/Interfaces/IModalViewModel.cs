using System;

namespace COMPASS.Common.Interfaces;

public interface IModalViewModel
{
    /// <summary>
    /// The title of the modal window
    /// </summary>
    string WindowTitle { get; }
    
    /// <summary>
    /// Optional minimum Window width
    /// </summary>
    int? WindowWidth { get; }
    
    // <summary>
    /// Optional maximum Window height
    /// </summary>
    int? WindowHeight { get; }
    
    /// <summary>
    /// Callback set by the modal that can be used to close the window from the vm
    /// </summary>
    Action CloseAction { get; set; }
}