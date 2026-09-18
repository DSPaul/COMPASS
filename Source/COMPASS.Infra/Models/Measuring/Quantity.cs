namespace COMPASS.Infra.Models.Measuring
{
    public class Quantity
    {
        private readonly List<Unit> _units;

        public Quantity(string name, List<Unit> units)
        {
            if(units == null || !units.Any(u => u.Value == 1))
            {
                throw new ArgumentException("Units list needs a unit for quantity 1.", nameof(units));
            }

            Name = name;
            _units = units;
        }

        public string Name { get;}

        public float ComputeValue(float value, Unit unit) => value * unit.Value;

        public float ParseValue(string value)
        {
            var parts = value.Split(' ', 2);

            float parsedValue;
            Unit parsedUnit = default;

            if (parts.Length == 0) return 0;
            else
            {
                bool parsed = float.TryParse(parts[0], out parsedValue);
                if (!parsed)
                {
                    throw new FormatException("The first part of the value must be a valid number.");
                }
            }

            if (parts.Length == 2)
            {
                string unitString = parts[1];
                bool parsed = false;
                foreach (var unit in _units)
                {
                    if (unitString == unit.Name || unitString == unit.Symbol)
                    {
                        parsedUnit = unit;
                        parsed = true;
                        break;
                    }
                }

                if(!parsed)
                {
                    throw new FormatException($"Unit '{unitString}' is not recognized.");
                }
            }

            if (parts.Length > 2)
            {
                throw new FormatException("Value must be in the format '<number> <unit>'.");
            }

            return ComputeValue(parsedValue, parsedUnit);
        }

        public string FormatValue(float value, int decimalPlaces = 2)
        {
            foreach (var unit in _units.OrderByDescending(u => u.Value))
            {
                if (value >= unit.Value)
                {
                    return $"{Math.Round(value / unit.Value, decimalPlaces)} {unit.Name}";
                }
            }

            return $"{Math.Round(value, decimalPlaces)} {_units.Single(u => u.Value == 1).Name}";
        }
    }
}
