using Avalonia;
using Avalonia.Controls.Primitives;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Adorners
{
    public class DropFilterAdorner : TemplatedControl
    {
        public static readonly StyledProperty<string> FormatProperty =
            AvaloniaProperty.Register<DropFilterAdorner, string>(nameof(Format), defaultValue: "Drop {0} here");

        public string Format
        {
            get => GetValue(FormatProperty);
            set
            {
                SetValue(FormatProperty, value);
                var parts = value.Split("{0}");
                Prefix = parts[0].Trim();
                Suffix = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
        }

        public static readonly StyledProperty<string> PrefixProperty =
            AvaloniaProperty.Register<DropFilterAdorner, string>(nameof(Prefix));

        public string Prefix
        {
            get => GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public static readonly StyledProperty<string> SuffixProperty =
            AvaloniaProperty.Register<DropFilterAdorner, string>(nameof(Suffix));

        public string Suffix
        {
            get => GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        public DropFilterAdorner(Filter draggedFilter)
        {
            DataContext = ModelVmFactory.GetFilterViewModel(draggedFilter);
        }
    }
}
