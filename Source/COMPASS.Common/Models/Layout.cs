using COMPASS.Common.Models.Enums;
using Material.Icons;

namespace COMPASS.Common.Models;

public class Layout
{
    public Layout(CodexLayout layout, string name, MaterialIconKind icon)
    {
        LayoutType = layout;
        Name = name;
        Icon = icon;
    }
    
    public CodexLayout LayoutType { get; }
    public string Name { get; }
    public MaterialIconKind Icon { get; }

    public string LongName => $"{Name} Layout";
}