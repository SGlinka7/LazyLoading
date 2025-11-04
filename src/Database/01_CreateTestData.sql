-- ==========================================
-- Skrypt tworzący testową bazę danych
-- ==========================================

USE [YourDatabaseName]
GO

-- Tabela przykładowa z produktami
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE [dbo].[Products]
    (
        [ProductId] INT IDENTITY(1,1) PRIMARY KEY,
        [ProductName] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(50),
        [Price] DECIMAL(18,2),
        [StockQuantity] INT,
        [CreatedDate] DATETIME DEFAULT GETDATE(),
        [IsActive] BIT DEFAULT 1
    )
END
GO

-- Wygeneruj testowe dane (30,000 rekordów)
IF NOT EXISTS (SELECT TOP 1 * FROM Products)
BEGIN
    DECLARE @i INT = 1
    DECLARE @categories TABLE (Name NVARCHAR(50))

    INSERT INTO @categories VALUES
        ('Electronics'), ('Books'), ('Clothing'), ('Food'), ('Toys'),
        ('Sports'), ('Home & Garden'), ('Automotive'), ('Health'), ('Beauty')

    WHILE @i <= 30000
    BEGIN
        INSERT INTO Products (ProductName, Category, Price, StockQuantity, IsActive)
        SELECT
            'Product ' + CAST(@i AS NVARCHAR(10)),
            (SELECT TOP 1 Name FROM @categories ORDER BY NEWID()),
            CAST((RAND() * 1000) AS DECIMAL(18,2)),
            CAST((RAND() * 500) AS INT),
            CASE WHEN RAND() > 0.1 THEN 1 ELSE 0 END

        SET @i = @i + 1

        IF @i % 1000 = 0
        BEGIN
            PRINT 'Wstawiono ' + CAST(@i AS NVARCHAR(10)) + ' rekordów...'
        END
    END

    PRINT 'Zakończono generowanie danych testowych'
END
GO

-- Indeksy dla wydajności
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Category')
BEGIN
    CREATE INDEX IX_Products_Category ON Products(Category)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CreatedDate')
BEGIN
    CREATE INDEX IX_Products_CreatedDate ON Products(CreatedDate DESC)
END
GO
