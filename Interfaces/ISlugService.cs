namespace MenuMint.Interfaces;

public interface ISlugService
{
    /// <summary>Pure text rule, no database: "Saffron &amp; Salt" becomes "saffron-and-salt".</summary>
    string Slugify(string? text);

    /// <summary>A slug nobody else uses yet: "saffron-and-salt", then "saffron-and-salt-2", "-3", and so on.</summary>
    Task<string> GenerateUniqueSlugAsync(string restaurantName);
}