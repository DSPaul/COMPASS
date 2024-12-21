using System.Xml;

namespace COMPASS.Common.Services.FileSystem
{
    public static class XmlService
    {
        public static XmlWriterSettings XmlWriteSettings { get; private set; } = new() { Indent = true };
    }
}
