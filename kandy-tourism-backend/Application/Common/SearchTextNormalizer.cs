using System.Globalization;
using System.Text;

namespace KandyTourism.Api.Application.Common;

/// <summary>
/// Normalises text for keyword search the same way the Angular prototype did: lower-case,
/// accents removed and apostrophes dropped, so "pauls" matches "St. Paul’s".
/// </summary>
public static class SearchTextNormalizer
{
    public static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (c is '\'' or '\u2019' || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static IReadOnlyList<string> ToTerms(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? []
            : Normalize(query).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .Take(10)
                .ToList();

    /// <summary>
    /// Descriptions and addresses are excluded on purpose: many mention nearby landmarks
    /// ("beside the Temple of the Tooth"), which would make keyword searches match unrelated places.
    /// </summary>
    public static string BuildAttractionSearchText(string name, string area, string categoryName, IEnumerable<string> tags) =>
        Normalize(string.Join(' ', new[] { name, area, categoryName }.Concat(tags)));
}
