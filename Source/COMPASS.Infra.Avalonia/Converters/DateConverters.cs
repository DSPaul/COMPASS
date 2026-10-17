using Avalonia.Data.Converters;
using System.Globalization;

namespace COMPASS.Infra.Avalonia.Converters;

public static class DateConverters
{
    /// <summary>
    /// Converts a weekday column index and the calendar's first day of the week into the first two letters of
    /// that day's abbreviated name in the current culture (e.g. "ma", "di" in Dutch, "Mo", "Tu" in English).
    /// Expects two bindings: the column index (int) and the first day of the week (<see cref="DayOfWeek"/>).
    /// </summary>
    /// <remarks>
    /// Needed because Avalonia's Calendar only exposes the culture's ShortestDayNames, which are single letters
    /// in many cultures and therefore ambiguous (e.g. two "D"s and two "Z"s in Dutch).
    /// </remarks>
    public static FuncMultiValueConverter<object?, string?> ColumnToTwoLetterDayName { get; } =
        new(bindingValues =>
        {
            var columnAndFirstDay = bindingValues.ToList();
            if (columnAndFirstDay.Count < 2
                || columnAndFirstDay[0] is not int column
                || columnAndFirstDay[1] is not DayOfWeek firstDayOfWeek)
            {
                return null;
            }

            int dayIndex = (column + (int)firstDayOfWeek) % 7;
            string abbreviatedDayName = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[dayIndex];
            return abbreviatedDayName.Length <= 2 ? abbreviatedDayName : abbreviatedDayName[..2];
        });
}
