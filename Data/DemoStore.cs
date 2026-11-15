using MenuMint.Models;

namespace MenuMint.Data;

// Temporary in-memory data for the frontend demo.
// Replaced by ADO.NET repositories in the backend step.
public static class DemoStore
{
    public static List<Category> Categories { get; } = new()
    {
        new() { CategoryId = 1, RestaurantId = 1, Name = "Starters",  DisplayOrder = 1 },
        new() { CategoryId = 2, RestaurantId = 1, Name = "Main Course", DisplayOrder = 2 },
        new() { CategoryId = 3, RestaurantId = 1, Name = "Desserts",  DisplayOrder = 3 },
        new() { CategoryId = 4, RestaurantId = 1, Name = "Beverages", DisplayOrder = 4 },
    };

    public static List<MenuItem> Items { get; } = new()
    {
    new() { MenuItemId = 1, CategoryId = 1, Name = "Bruschetta",       Description = "Tomato, basil, olive oil",   Price = 650,  Emoji = "🥖", ImagePath = "/images/dishes/bruschetta.jpg" },
    new() { MenuItemId = 2, CategoryId = 1, Name = "Crispy Wings",     Description = "Smoky BBQ glaze",            Price = 850,  Emoji = "🍗", ImagePath = "/images/dishes/crispy-wings.jpg" },
    new() { MenuItemId = 3, CategoryId = 2, Name = "Margherita Pizza", Description = "Fresh basil, mozzarella",    Price = 1450, Emoji = "🍕", ImagePath = "/images/dishes/margherita-pizza.jpg" },
    new() { MenuItemId = 4, CategoryId = 2, Name = "Smash Burger",     Description = "Double patty, house sauce",  Price = 1250, Emoji = "🍔", ImagePath = "/images/dishes/smash-burger.jpg" },
    new() { MenuItemId = 5, CategoryId = 2, Name = "Alfredo Pasta",    Description = "Creamy parmesan sauce",      Price = 1350, Emoji = "🍝", ImagePath = "/images/dishes/alfredo-pasta.jpg", IsAvailable = false },
    new() { MenuItemId = 6, CategoryId = 3, Name = "Lava Cake",        Description = "Warm chocolate center",      Price = 750,  Emoji = "🍰", ImagePath = "/images/dishes/lava-cake.jpg" },
    new() { MenuItemId = 7, CategoryId = 3, Name = "Mango Kulfi",      Description = "Pistachio topping",          Price = 450,  Emoji = "🍨", ImagePath = "/images/dishes/mango-kulfi.jpg" },
    new() { MenuItemId = 8, CategoryId = 4, Name = "Mint Margarita",   Description = "Fresh mint, lime, soda",     Price = 550,  Emoji = "🍹", ImagePath = "/images/dishes/mint-margarita.jpg" },
    new() { MenuItemId = 9, CategoryId = 4, Name = "Cold Coffee",      Description = "Iced, creamy, bold",         Price = 600,  Emoji = "☕", ImagePath = "/images/dishes/cold-coffee.jpg" },
    };

    public static Restaurant Restaurant { get; } = new()
    {
        RestaurantId = 1,
        OwnerUserId = "demo",
        Name = "Saffron & Salt",
        Slug = "saffron-and-salt",
        Description = "Wood-fired pizza, smash burgers and desserts worth the detour.",
        Address = "",
        Phone = ""
    };

    public static bool QrGenerated { get; set; }
    public static List<PlatformEntry> Platform { get; } = BuildPlatform();

    private static List<PlatformEntry> BuildPlatform()
    {
        var now = DateTime.UtcNow;

        PlatformEntry Make(int id, string name, string slug, string owner, string email,
                           int daysAgo, int items, int cats, bool active = true) => new()
                           {
                               Restaurant = new Restaurant
                               {
                                   RestaurantId = id,
                                   Name = name,
                                   Slug = slug,
                                   IsActive = active,
                                   OwnerUserId = "sample-" + id
                               },
                               OwnerName = owner,
                               OwnerEmail = email,
                               Joined = now.AddDays(-daysAgo),
                               Items = items,
                               Categories = cats
                           };

        return new List<PlatformEntry>
    {
        new() { Restaurant = Restaurant, OwnerName = "Hifza Rasool", OwnerEmail = "owner@menumint.test",
                Joined = now.AddDays(-12), IsMain = true },
        Make(2, "Karachi Biryani House", "karachi-biryani-house", "Ahmed Raza",   "ahmed@kbh.pk",       3,   24, 6),
        Make(3, "Lahore Tikka Corner",   "lahore-tikka-corner",   "Sana Malik",   "sana@tikka.pk",      20,  31, 7),
        Make(4, "Brew & Bite Café",      "brew-and-bite",         "Usman Tariq",  "usman@brewbite.com", 35,  18, 5),
        Make(5, "The Green Fork",        "the-green-fork",        "Maria Khan",   "maria@greenfork.pk", 58,  27, 6),
        Make(6, "Spice Route",           "spice-route",           "Bilal Hussain","bilal@spice.pk",     75,  14, 4, active: false),
        Make(7, "Dessert Dreams",        "dessert-dreams",        "Ayesha Noor",  "ayesha@dd.pk",       110, 22, 3),
        Make(8, "Burger Lab",            "burger-lab",            "Hamza Ali",    "hamza@burgerlab.pk", 140, 16, 4),
    };
    }

    private static int _nextCategoryId = 5;
    private static int _nextItemId = 10;

    public static int NextCategoryId() => _nextCategoryId++;
    public static int NextItemId() => _nextItemId++;
}