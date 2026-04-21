using System;
using System.Collections.Generic;
using System.Text;

namespace COMPASS.Infra.Models
{
    public class NotificationOption
    {
        public string Identifier { get; }
        public string Label { get;}

        public bool IsChecked { get; set;}

        public NotificationOption(string identifier, string label, bool isChecked)
        {
            Identifier = identifier;
            Label = label;
            IsChecked = isChecked;
        }
    }
}
