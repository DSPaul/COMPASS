using Avalonia.Controls;
using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Windows.Services;

public class UIService : IUIService
{
    public GridLength WindowControlsSpacing => new GridLength(135);
}