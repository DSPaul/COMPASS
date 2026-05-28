namespace COMPASS.Common.Models
{
    public class Dependency
    {
        public Dependency(string name, string link, string description, string license)
        {
            Name = name;
            Link = link;
            Description = description;
            License = license;
        }

        public string Name { get; }
        public string Link { get; }
        public string Description { get; }
        public string License { get; }
    }
}
