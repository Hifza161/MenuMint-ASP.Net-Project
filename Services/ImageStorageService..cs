using MenuMint.Interfaces;

namespace MenuMint.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(IWebHostEnvironment env, ILogger<ImageStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    private static readonly Dictionary<ImageFolder, (string Folder, int MaxBytes, string Label)> Rules = new()
    {
        [ImageFolder.Items] = ("items", 2 * 1024 * 1024, "Image"),
        [ImageFolder.Logos] = ("logos", 2 * 1024 * 1024, "Logo"),
        [ImageFolder.Covers] = ("covers", 3 * 1024 * 1024, "Cover photo"),
    };

    public async Task<string?> ValidateAsync(IFormFile file, ImageFolder folder)
    {
        var rule = Rules[folder];

        if (file.Length == 0) return "The file is empty.";
        if (file.Length > rule.MaxBytes) return $"{rule.Label} must be {rule.MaxBytes / (1024 * 1024)} MB or smaller.";
        if (await DetectExtensionAsync(file) is null) return "Only JPG, PNG or WEBP images are allowed.";

        return null;
    }

    public async Task<string> SaveAsync(IFormFile file, ImageFolder folder)
    {
        var rule = Rules[folder];

        // The extension comes from what the file really is, not from the name the browser sent
        var ext = await DetectExtensionAsync(file)
            ?? throw new InvalidOperationException("Not a supported image. Call ValidateAsync first.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", rule.Folder);
        Directory.CreateDirectory(dir);

        var fileName = Guid.NewGuid().ToString("N") + ext;
        await using (var stream = File.Create(Path.Combine(dir, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{rule.Folder}/{fileName}";
    }

    public void Delete(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath)) return;

        foreach (var rule in Rules.Values)
        {
            if (!webPath.StartsWith($"/uploads/{rule.Folder}/", StringComparison.OrdinalIgnoreCase)) continue;

            // GetFileName strips any "../" tricks, so only a file inside that one folder can be deleted
            var full = Path.Combine(_env.WebRootPath, "uploads", rule.Folder, Path.GetFileName(webPath));
            try
            {
                if (File.Exists(full)) File.Delete(full);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete {Path}", full);   // a leftover file must not break the request
            }
            return;
        }
        // Anything else (like the demo photos in /images) is deliberately ignored
    }

    // Reads the first bytes of the file (its "magic number") to find out what it really is
    private static async Task<string?> DetectExtensionAsync(IFormFile file)
    {
        var h = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(h.AsMemory(), 12, throwOnEndOfStream: false);
        if (read < 12) return null;

        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
        if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return ".png";
        if (h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&     // "RIFF"
            h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50)     // "WEBP"
            return ".webp";

        return null;
    }
}