namespace MenuMint.Interfaces;

public enum ImageFolder
{
    Items,    // dish photos     (max 2 MB)
    Logos,    // restaurant logo (max 2 MB)
    Covers    // menu cover      (max 3 MB)
}

public interface IImageStorageService
{
    /// <summary>Returns an error message to show the user, or null when the file is fine.</summary>
    Task<string?> ValidateAsync(IFormFile file, ImageFolder folder);

    /// <summary>Saves the file under wwwroot/uploads and returns its web path, e.g. /uploads/items/abc.jpg.</summary>
    Task<string> SaveAsync(IFormFile file, ImageFolder folder);

    /// <summary>Deletes an uploaded file. Paths outside /uploads (like the demo photos in /images) are never touched.</summary>
    void Delete(string? webPath);
}