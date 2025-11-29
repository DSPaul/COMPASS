using Avalonia.Controls;
using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Linux.Services;

public class UIService : IUIService
{
    public GridLength WindowControlsSpacing => new GridLength(0); // window controls are in separate title bar
}