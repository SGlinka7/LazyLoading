-- ==========================================
-- Procedury składowane z paginacją
-- ==========================================

USE [YourDatabaseName]
GO

-- ==========================================
-- 1. Podstawowa procedura z paginacją
-- ==========================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'Products_SelectView_Paged')
    DROP PROCEDURE [dbo].[Products_SelectView_Paged]
GO

CREATE PROCEDURE [dbo].[Products_SelectView_Paged]
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Oblicz offset
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize

    -- Pobierz całkowitą liczbę rekordów
    SELECT @TotalCount = COUNT(*)
    FROM Products
    WHERE IsActive = 1

    -- Pobierz dane dla strony
    SELECT
        ProductId,
        ProductName,
        Category,
        Price,
        StockQuantity,
        CreatedDate,
        IsActive
    FROM Products
    WHERE IsActive = 1
    ORDER BY ProductId
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

-- ==========================================
-- 2. Procedura z filtrowaniem i paginacją
-- ==========================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'Products_SelectByCategory_Paged')
    DROP PROCEDURE [dbo].[Products_SelectByCategory_Paged]
GO

CREATE PROCEDURE [dbo].[Products_SelectByCategory_Paged]
    @Category NVARCHAR(50) = NULL,
    @SearchText NVARCHAR(100) = NULL,
    @MinPrice DECIMAL(18,2) = NULL,
    @MaxPrice DECIMAL(18,2) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize

    -- Pobierz całkowitą liczbę rekordów spełniających kryteria
    SELECT @TotalCount = COUNT(*)
    FROM Products
    WHERE IsActive = 1
        AND (@Category IS NULL OR Category = @Category)
        AND (@SearchText IS NULL OR ProductName LIKE '%' + @SearchText + '%')
        AND (@MinPrice IS NULL OR Price >= @MinPrice)
        AND (@MaxPrice IS NULL OR Price <= @MaxPrice)

    -- Pobierz dane dla strony
    SELECT
        ProductId,
        ProductName,
        Category,
        Price,
        StockQuantity,
        CreatedDate
    FROM Products
    WHERE IsActive = 1
        AND (@Category IS NULL OR Category = @Category)
        AND (@SearchText IS NULL OR ProductName LIKE '%' + @SearchText + '%')
        AND (@MinPrice IS NULL OR Price >= @MinPrice)
        AND (@MaxPrice IS NULL OR Price <= @MaxPrice)
    ORDER BY ProductName
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

-- ==========================================
-- 3. Procedura z sortowaniem dynamicznym
-- ==========================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'Products_SelectView_PagedWithSort')
    DROP PROCEDURE [dbo].[Products_SelectView_PagedWithSort]
GO

CREATE PROCEDURE [dbo].[Products_SelectView_PagedWithSort]
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @SortColumn NVARCHAR(50) = 'ProductId',
    @SortDirection NVARCHAR(4) = 'ASC',
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    DECLARE @SQL NVARCHAR(MAX)

    -- Pobierz całkowitą liczbę rekordów
    SELECT @TotalCount = COUNT(*)
    FROM Products
    WHERE IsActive = 1

    -- Walidacja kolumny sortowania (zabezpieczenie przed SQL injection)
    IF @SortColumn NOT IN ('ProductId', 'ProductName', 'Category', 'Price', 'StockQuantity', 'CreatedDate')
        SET @SortColumn = 'ProductId'

    IF @SortDirection NOT IN ('ASC', 'DESC')
        SET @SortDirection = 'ASC'

    -- Dynamiczne sortowanie
    SET @SQL = N'
    SELECT
        ProductId,
        ProductName,
        Category,
        Price,
        StockQuantity,
        CreatedDate,
        IsActive
    FROM Products
    WHERE IsActive = 1
    ORDER BY ' + QUOTENAME(@SortColumn) + ' ' + @SortDirection + '
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY'

    EXEC sp_executesql @SQL,
        N'@Offset INT, @PageSize INT',
        @Offset = @Offset,
        @PageSize = @PageSize
END
GO

-- ==========================================
-- 4. Template procedury do konwersji istniejących
-- ==========================================
/*
SZABLON DO KONWERSJI ISTNIEJĄCYCH PROCEDUR ...SelectView:

Twoja stara procedura:
-----------------------
CREATE PROCEDURE [dbo].[YourTable_SelectView]
AS
BEGIN
    SELECT * FROM YourTable
END

Nowa procedura z paginacją:
---------------------------
CREATE PROCEDURE [dbo].[YourTable_SelectView_Paged]
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize

    -- Oblicz total count
    SELECT @TotalCount = COUNT(*) FROM YourTable

    -- Pobierz stronę danych
    SELECT *
    FROM YourTable
    ORDER BY [YourPrimaryKey]  -- WAŻNE: zawsze dodaj ORDER BY!
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END

UWAGI:
1. Zawsze dodawaj OUTPUT parameter @TotalCount
2. Zawsze używaj ORDER BY (wymagane dla OFFSET/FETCH)
3. Używaj indeksów na kolumnach w ORDER BY dla wydajności
4. Offset/Fetch dostępne od SQL Server 2012+
*/

PRINT 'Procedury z paginacją utworzone pomyślnie!'
GO
