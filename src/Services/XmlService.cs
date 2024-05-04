using System.Xml;

namespace COMPASS.Services
{
    public class XmlService
    {
        public static XmlWriterSettings XmlWriteSettings { get; private set; } = new() { Indent = true };
    }
}
