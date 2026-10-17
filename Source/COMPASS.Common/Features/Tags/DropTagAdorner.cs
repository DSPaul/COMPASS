using Avalonia;
using Avalonia.Controls.Primitives;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Tags
{
    public class DropTagAdorner : TemplatedControl
    {
        public static readonly StyledProperty<string> FormatProperty =
            AvaloniaProperty.Register<DropTagAdorner, string>(nameof(Format), defaultValue: "Drop {0} here");

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
            AvaloniaProperty.Register<DropTagAdorner, string>(nameof(Prefix));

        public string Prefix
        {
            get => GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public static readonly StyledProperty<string> SuffixProperty =
            AvaloniaProperty.Register<DropTagAdorner, string>(nameof(Suffix), defaultValue: string.Empty);

        public string Suffix
        {
            get => GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        public DropTagAdorner(Tag draggedTag)
        {
            DataContext = draggedTag;
        }
    }
}
