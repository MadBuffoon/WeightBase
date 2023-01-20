using System;

namespace WeightBase.Tools;

public static class Helper
{
    internal static string FormatNumberSimple(float number)
    {
        int mag = (int)(Math.Floor(Math.Log10(number)) / 3); // Truncates to 6, divides to 2
        double divisor = Math.Pow(10, mag * 3);

        double shortNumber = number / divisor;

        string suffix;
        switch (mag)
        {
            default:
                return number.ToString();
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

        return shortNumber.ToString("N2") + suffix;
    }
}