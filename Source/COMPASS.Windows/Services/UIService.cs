using Avalonia.Controls;
using COMPASS.Infra.Avalonia.Application;

namespace COMPASS.Windows.Services;

public class UIService : IUIService
{
    public GridLength WindowControlsSpacing => new GridLength(140);
}