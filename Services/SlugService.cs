using MenuMint.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MenuMint.Services;

public class SlugService : ISlugService
{
    private const int MaxBaseLength = 100;   // the Slug column is 120; the rest is room for "-2", "-3" ...
    private readonly IRestaurantRepository _restaurants;

    public SlugService(IRestaurantRepository restaurants) => _restaurants = restaurants;

    public string Slugify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // "Café" becomes "Cafe": split accents off the letters, then drop the accent marks
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);

        // Same steps, in the same order, as slugify() in wwwroot/js/auth.js
        var s = sb.ToString().ToLowerInvariant().Trim().Replace("&", " and ");
        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");   // drop everything except letters, digits, spaces and hyphens
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, "-+", "-");
        return s.Trim('-');
    }

    public async Task<string> GenerateUniqueSlugAsync(string restaurantName)
    {
        var baseSlug = Slugify(restaurantName);
        if (baseSlug.Length == 0) baseSlug = "restaurant";            // e.g. a name written only in Urdu
        if (baseSlug.Length > MaxBaseLength) baseSlug = baseSlug[..MaxBaseLength].TrimEnd('-');

        var candidate = baseSlug;
        for (var n = 2; await _restaurants.SlugExistsAsync(candidate); n++)
        {
            if (n > 1000) throw new InvalidOperationException("Could not find a free slug.");
            candidate = $"{baseSlug}-{n}";
        }
        return candidate;
    }
}