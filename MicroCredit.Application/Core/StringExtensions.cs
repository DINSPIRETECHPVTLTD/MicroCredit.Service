namespace MicroCredit.Application.Core;

public static class StringExtensions
{
    /// <summary>Returns null if the string is null or whitespace; otherwise returns the original string.</summary>
    public static string? NullIfEmpty(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
