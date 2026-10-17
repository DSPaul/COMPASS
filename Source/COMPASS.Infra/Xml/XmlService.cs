using System.Xml;
namespace COMPASS.Infra.Xml
{
    public static class XmlService
    {
        public static XmlWriterSettings XmlWriteSettings { get; private set; } = new() { Indent = true };
    }
}
