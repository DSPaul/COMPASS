using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;

namespace COMPASS.Common.Adorners
{
    public class FileDropAdorner : TemplatedControl
    {
        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<FileDropAdorner, string>(nameof(Message));

        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public FileDropAdorner(IEnumerable<IStorageItem> items)
        {
            int folderCount = items.OfType<IStorageFolder>().Count();
            int fileCount = items.Count() - folderCount;

            Message = BuildMessage(fileCount, folderCount);
        }

        private static string BuildMessage(int fileCount, int folderCount)
        {
            if (fileCount > 0 && folderCount > 0)
                return $"Import {Pluralize(fileCount, "file")} and {Pluralize(folderCount, "folder")}";

            if (folderCount > 0)
                return $"Import {Pluralize(folderCount, "folder")}";

            return $"Import {Pluralize(fileCount, "file")}";
        }

        private static string Pluralize(int count, string noun) =>
            count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
