using System;
using Avalonia.Controls;

namespace COMPASS.Common.Interfaces.ViewModels;

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

    SizeToContent SizeToContent => SizeToContent.WidthAndHeight;
    
    /// <summary>
    /// Callback set by the modal that can be used to close the window from the vm
    /// </summary>
    Action CloseAction { get; set; }
}