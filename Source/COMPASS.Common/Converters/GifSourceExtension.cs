using Avalonia.Labs.Gif;
using Avalonia.Markup.Xaml;

namespace COMPASS.Common.Converters;

public class GifSourceExtension(string uri) : MarkupExtension
{
    public string Uri { get; set; } = uri;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => GifStreamSource.FromUriString(Uri);
}
