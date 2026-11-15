-- MenuMint business tables.
-- Idempotent: safe to run on every start (each table is created only if missing).
-- Identity tables (AspNetUsers, ...) come from EF Core migrations, which run first.

IF OBJECT_ID(N'dbo.Restaurants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Restaurants (
        RestaurantId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Restaurants PRIMARY KEY,
        OwnerUserId    NVARCHAR(450) NOT NULL,
        Name           NVARCHAR(100) NOT NULL,
        Slug           NVARCHAR(120) NOT NULL,
        Description    NVARCHAR(300) NOT NULL CONSTRAINT DF_Restaurants_Description DEFAULT N'',
        Address        NVARCHAR(200) NOT NULL CONSTRAINT DF_Restaurants_Address     DEFAULT N'',
        Phone          NVARCHAR(20)  NOT NULL CONSTRAINT DF_Restaurants_Phone       DEFAULT N'',
        LogoPath       NVARCHAR(260) NULL,
        CoverPath      NVARCHAR(260) NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_Restaurants_IsActive    DEFAULT 1,
        QrGeneratedAt  DATETIME2     NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_Restaurants_CreatedAt   DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Restaurants_Owner FOREIGN KEY (OwnerUserId) REFERENCES dbo.AspNetUsers (Id),
        CONSTRAINT UQ_Restaurants_Slug  UNIQUE (Slug),
        CONSTRAINT UQ_Restaurants_Owner UNIQUE (OwnerUserId)
    );
END;

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        CategoryId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        RestaurantId   INT           NOT NULL,
        Name           NVARCHAR(50)  NOT NULL,
        DisplayOrder   INT           NOT NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1,
        CONSTRAINT FK_Categories_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (RestaurantId) ON DELETE CASCADE,
        CONSTRAINT UQ_Categories_Name UNIQUE (RestaurantId, Name)
    );
END;

IF OBJECT_ID(N'dbo.MenuItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MenuItems (
        MenuItemId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MenuItems PRIMARY KEY,
        RestaurantId   INT            NOT NULL,
        CategoryId     INT            NOT NULL,
        Name           NVARCHAR(80)   NOT NULL,
        Description    NVARCHAR(200)  NOT NULL CONSTRAINT DF_MenuItems_Description DEFAULT N'',
        Price          DECIMAL(10,2)  NOT NULL,
        Emoji          NVARCHAR(16)   NOT NULL CONSTRAINT DF_MenuItems_Emoji DEFAULT N'',
        ImagePath      NVARCHAR(260)  NULL,
        IsAvailable    BIT            NOT NULL CONSTRAINT DF_MenuItems_IsAvailable DEFAULT 1,
        CONSTRAINT FK_MenuItems_Restaurant FOREIGN KEY (RestaurantId) REFERENCES dbo.Restaurants (RestaurantId),
        CONSTRAINT FK_MenuItems_Category   FOREIGN KEY (CategoryId)   REFERENCES dbo.Categories (CategoryId),
        CONSTRAINT CK_MenuItems_Price CHECK (Price > 0)
    );

    CREATE INDEX IX_MenuItems_Restaurant_Category ON dbo.MenuItems (RestaurantId, CategoryId);
END;