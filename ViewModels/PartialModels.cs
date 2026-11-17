namespace MenuMint.ViewModels;

/// <summary>One stat card. Tone: "" (mint), "coral" or "butter".</summary>
public record StatCardVm(string Icon, string Tone, int Value, string Label);

public record CategoryCardVm(CategoryRowVm Row, int Position, int Total);

/// <summary>An empty-state message with an optional button.</summary>
public record EmptyStateVm(
    string Emoji,
    string Title,
    string Text,
    string? ButtonText = null,
    string? ButtonUrl = null,
    bool Compact = false);   // Compact = no card of its own, for use inside another card

/// <summary>Settings for the drag-and-drop image box.</summary>
public class ImageUploaderVm
{
    public string Name { get; init; } = "";          // property name of the IFormFile, e.g. "LogoFile"
    public string RemoveName { get; init; } = "";    // property name of the remove flag, e.g. "RemoveLogo"
    public string Label { get; init; } = "";
    public string Note { get; init; } = "";          // grey text after the label, e.g. "(optional)"
    public string Prompt { get; init; } = "Click or drag a photo here";
    public string Hint { get; init; } = "JPG, PNG or WEBP · max 2 MB";
    public string Icon { get; init; } = "bi-cloud-arrow-up";
    public string? ExistingPath { get; init; }
    public string? Error { get; init; }              // server-side error message, if any
    public int MaxMb { get; init; } = 2;
    public int PreviewHeight { get; init; } = 170;
    public string Fit { get; init; } = "cover";      // "cover" for photos, "contain" for logos
}