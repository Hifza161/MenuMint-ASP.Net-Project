namespace MenuMint.Models;

// One row in the admin's "all restaurants" list (demo data for now)
public class PlatformEntry
{
    public Restaurant Restaurant { get; set; } = null!;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public DateTime Joined { get; set; }
    public int Items { get; set; }
    public int Categories { get; set; }
    public bool IsMain { get; set; }   // the real demo restaurant (counts are live)
}