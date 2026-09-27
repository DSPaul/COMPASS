namespace COMPASS.Infra.Notifications
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
