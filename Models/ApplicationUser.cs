using Microsoft.AspNetCore.Identity;

namespace MenuMint.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;   // admin can activate / deactivate
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}