using System;
using System.ComponentModel;
using System.Windows.Markup;

namespace COMPASS.Tools
{
    // From https://stackoverflow.com/questions/6154050/access-displayname-in-xaml
    public class DisplayNameExtension : MarkupExtension
    {
        public Type Type { get; set; }

        public string PropertyName { get; set; }

        public DisplayNameExtension() { }
        public DisplayNameExtension(string propertyName)
        {
            PropertyName = propertyName;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // (This code has zero tolerance)
            var prop = Type.GetProperty(PropertyName);
            var attributes = prop.GetCustomAttributes(typeof(DisplayNameAttribute), false);
            return (attributes[0] as DisplayNameAttribute).DisplayName;
        }
    }
}
