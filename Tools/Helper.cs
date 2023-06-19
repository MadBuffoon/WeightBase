using System;

namespace WeightBase.Tools
{
    public static class Helper
    {
        internal static string FormatNumberSimple(float number)
        {
            double shortNumber;
            string suffix;
            CalculateShortNumberAndSuffix(number, out shortNumber, out suffix);
            return shortNumber.ToString("N2") + suffix;
        }

        internal static string FormatNumberSimpleNoDecimal(float number)
        {
            double shortNumber;
            string suffix;
            CalculateShortNumberAndSuffix(number, out shortNumber, out suffix);
            return shortNumber.ToString("N0") + suffix;
        }

        private static void CalculateShortNumberAndSuffix(float number, out double shortNumber, out string suffix)
        {
            int mag = (int)(Math.Log10(number) / 3);
            double divisor = Math.Pow(10, mag * 3);

            shortNumber = number / divisor;
            suffix = "";  // Initially set the suffix to an empty string

            switch (mag)
            {
                case 1:
                    suffix = "k";
                    break;
                case 2:
                    suffix = "m";
                    break;
                case 3:
                    suffix = "b";
                    break;
            }
        }
    }
}