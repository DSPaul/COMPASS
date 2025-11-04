namespace COMPASS.Common.Models
{
    public interface IDealsWithTabControl
    {
        int SelectedTab { get; set; }
        bool Collapsed { get; set; }
        int PrevSelectedTab { get; set; }
    }

    public interface IHasCodexMetadata { }
}
