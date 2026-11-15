using System.Reflection;
using Avalonia.Platform.Storage;

namespace COMPASS.UnitTests.Infra.Avalonia.DragDrop;

/// <summary>
/// Stands in for items handed over by another app during a drag and drop.
/// </summary>
/// <remarks>
/// Built as dispatch proxies because Avalonia does not let user code implement
/// <see cref="IStorageItem"/> directly.
/// </remarks>
internal static class FakeDroppedItems
{
    /// <summary>
    /// An item that is a real file on this machine.
    /// </summary>
    public static IStorageItem OnDisk(string localPath) =>
        Create<IStorageItem>(new Uri(localPath), Path.GetFileName(localPath), () => throw new NotSupportedException());

    /// <summary>
    /// A virtual file, as the shell hands over for an entry dragged out of a ZIP folder or a mail
    /// attachment: no path on disk, but its contents can be read. Like Avalonia's own stream for it,
    /// the contents can only be read synchronously.
    /// </summary>
    public static IStorageFile VirtualFile(string name, string contents = "") =>
        Create<IStorageFile>(VirtualPath(name), name, () => new SyncReadOnlyStream(System.Text.Encoding.UTF8.GetBytes(contents)));

    /// <summary>
    /// A virtual file whose contents cannot be read.
    /// </summary>
    public static IStorageFile UnreadableVirtualFile(string name) =>
        Create<IStorageFile>(VirtualPath(name), name, () => throw new IOException($"{name} cannot be read"));

    /// <summary>
    /// An item with no path on disk that is not a file either.
    /// </summary>
    public static IStorageItem VirtualItemWithoutContents(string name) =>
        Create<IStorageItem>(VirtualPath(name), name, () => throw new NotSupportedException());

    private static Uri VirtualPath(string name) => new($"virtual-file:///{Uri.EscapeDataString(name)}");

    /// <summary>
    /// Mirrors Avalonia's stream over a shell virtual file: it implements only a synchronous Read,
    /// because the OLE stream behind it may only be used on the thread that received the drop. The
    /// base Stream would run any async read on a pool thread, which fails there with
    /// RPC_E_WRONG_THREAD, so async reads throw here to catch that.
    /// </summary>
    private sealed class SyncReadOnlyStream(byte[] contents) : MemoryStream(contents, writable: false)
    {
        private const string AsyncReadMessage =
            "Virtual file streams must be read synchronously: an async read runs on a pool thread, where the OLE stream fails";

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(AsyncReadMessage);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(AsyncReadMessage);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            throw new InvalidOperationException(AsyncReadMessage);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(AsyncReadMessage);
    }

    private static T Create<T>(Uri path, string name, Func<Stream> openRead) where T : class
    {
        T droppedItem = DispatchProxy.Create<T, StorageItemProxy>();
        ((StorageItemProxy)(object)droppedItem).Configure(path, name, openRead);
        return droppedItem;
    }

    private class StorageItemProxy : DispatchProxy
    {
        private Uri _itemPath = new("virtual-file:///item");
        private string _itemName = "item";
        private Func<Stream> _openRead = () => throw new NotSupportedException();

        public void Configure(Uri path, string name, Func<Stream> openRead)
        {
            _itemPath = path;
            _itemName = name;
            _openRead = openRead;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Path" => _itemPath,
            "get_Name" => _itemName,
            "get_CanBookmark" => false,
            "OpenReadAsync" => Task.FromResult(_openRead()),
            "Dispose" => null,
            _ => throw new NotSupportedException($"{targetMethod?.Name} is not used by a drop handler")
        };
    }
}
