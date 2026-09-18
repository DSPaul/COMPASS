namespace COMPASS.Infra.Models.Measuring
{
    public static class Quantities
    {
        public static Quantity FileSize => field ??= new("File Size", [
                new Unit("bytes", "B", 1),
                new Unit("kilobytes", "kB", 1000),
                new Unit("megabytes", "MB", 1000 * 1000),
                new Unit("gigabytes", "GB", 1000 * 1000 * 1000)
            ]);
        

        public static Quantity Items(string name = "Items") => new(name, [new Unit(name, 1)]);
    }
}
