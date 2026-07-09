using System.Diagnostics.CodeAnalysis;

namespace COMPASS.Common.Services
{
    public static class ValidationService
    {
        
        public static bool IsValidISBN([NotNullWhen(true)] string? isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn)) return false;

            // length must be 10
            int n = isbn.Length;
            int sum = 0;

            switch (n)
            {
                case 10:
                    //https://www.geeksforgeeks.org/program-check-isbn/

                    // Computing weighted sum of first 9 digits
                    for (int i = 0; i < 9; i++)
                    {
                        int digit = isbn[i] - '0';
                        if (0 > digit || 9 < digit)
                            return false;
                        sum += digit * (10 - i);
                    }

                    // Checking last digit.
                    char last = isbn[9];
                    if (last != 'X' && (last < '0' || last > '9'))
                        return false;

                    // If last digit is 'X', add 10 to sum, else add its value.
                    sum += (last == 'X') ? 10 : (last - '0');

                    // Return true if weighted sum of digits is divisible by 11.
                    return sum % 11 == 0;

                case 13:
                    for (int i = 0; i < 13; i++)
                    {
                        int digit = isbn[i] - '0';
                        if (0 > digit || 9 < digit)
                            return false;
                        sum += digit * (1 + (2 * (i % 2)));
                    }
                    // Return true if weighted sum of digits is divisible by 10.
                    return sum % 10 == 0;

                default:
                    return false;
            }
        }
    }
}
