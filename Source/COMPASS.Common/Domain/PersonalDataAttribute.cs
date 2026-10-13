namespace COMPASS.Common.Domain
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PersonalDataAttribute : Attribute
    {
        public PersonalDataAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; }
    }
}
