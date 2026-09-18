namespace COMPASS.Infra.Models.Measuring
{
    public struct Unit
    {
        public Unit(string name, string symbol, float value)
        {
            Name = name;
            Symbol = symbol;
            Value = value;
        }

        public Unit(string unit, float value)
        {
            Name = unit;
            Symbol = unit;
            Value = value;
        }

        public string Name { get; set; }
        public string Symbol { get; set; }

        public float Value { get; set; }
    }
}
