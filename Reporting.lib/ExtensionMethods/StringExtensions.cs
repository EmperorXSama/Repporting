namespace Reporting.lib.ExtensionMethods;

public static class StringExtensions
{
    public static string? GetValueBetweenBrackets(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        int startIndex = input.IndexOf('[') + 1;
        int endIndex = input.IndexOf(']');

        if (startIndex > 0 && endIndex > startIndex)
        {
            return input.Substring(startIndex, endIndex - startIndex);
        }

        return "Unknown Error";
    }
}