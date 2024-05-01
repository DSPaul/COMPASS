using System.Xml;

namespace COMPASS.Common.Services
{
    public class XmlService
    {
        public static XmlWriterSettings XmlWriteSettings { get; private set; } = new() { Indent = true };
    }
}
