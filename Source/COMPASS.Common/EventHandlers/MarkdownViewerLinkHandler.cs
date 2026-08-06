using MarkView.Avalonia;
using System.Diagnostics;

namespace COMPASS.Common.EventHandlers
{
    /// <summary>
    /// Registers a class handler so every <see cref="MarkdownViewer"/> opens absolute links in the default browser.
    /// </summary>
    public static class MarkdownViewerLinkHandler
    {
        private static bool _registered;

        public static void EnsureRegistered()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;

            MarkdownViewer.LinkClickedEvent.AddClassHandler<MarkdownViewer>((_, e) =>
            {
                if (Uri.TryCreate(e.Url, UriKind.Absolute, out Uri? linkUrl))
                {
                    Process.Start(new ProcessStartInfo(linkUrl.AbsoluteUri) { UseShellExecute = true });
                }
            });
        }
    }
}
