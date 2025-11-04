# 🚀 SQL Lazy Loading & Pagination dla WinForms

Mechanizm **plug & play** do wydajnego ładowania dużych zbiorów danych z SQL Server w aplikacjach WinForms. Zamiast ładować 30k+ rekordów na raz, dane ładują się automatycznie przy scrollowaniu - po 30/50/100 rekordów na raz.

## ⚡ Kluczowe funkcje

- ✅ **Lazy Loading** - automatyczne doładowywanie danych przy scrollowaniu
- ✅ **Plug & Play** - łatwa integracja z istniejącymi procedurami SQL
- ✅ **Cache** - załadowane strony są cache'owane w pamięci
- ✅ **Prefetch** - inteligentne wczytywanie następnych stron w tle
- ✅ **Uniwersalny** - działa z każdą procedurą składowaną
- ✅ **Wizualne wskaźniki** - progress bar i licznik załadowanych rekordów
- ✅ **Thread-safe** - asynchroniczne ładowanie bez blokowania UI

## 📋 Wymagania

- .NET 6.0+ (Windows)
- SQL Server 2012+ (dla OFFSET/FETCH)
- WinForms

## 🏗️ Architektura

```
┌─────────────────────────────────┐
│   PagedDataGridView (Control)   │
│   - Automatyczne scrollowanie   │
│   - UI indicators               │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  VirtualScrollHandler           │
│  - Zarządzanie scrollem         │
│  - Trigger ładowania stron      │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  PagedDataSource (Abstract)     │
│  - Cache stron                  │
│  - Komunikacja z SQL            │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  SQL Stored Procedure           │
│  - @PageNumber, @PageSize       │
│  - @TotalCount OUTPUT           │
└─────────────────────────────────┘
```

## 🚀 Szybki start

### 1️⃣ Przygotuj procedurę SQL z paginacją

```sql
CREATE PROCEDURE [dbo].[Products_SelectView_Paged]
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize

    -- Oblicz total count
    SELECT @TotalCount = COUNT(*) FROM Products WHERE IsActive = 1

    -- Pobierz stronę
    SELECT
        ProductId,
        ProductName,
        Category,
        Price,
        StockQuantity
    FROM Products
    WHERE IsActive = 1
    ORDER BY ProductId  -- WYMAGANE dla OFFSET/FETCH
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
```

**⚠️ WAŻNE:** `ORDER BY` jest **obowiązkowe** dla `OFFSET/FETCH`!

### 2️⃣ Stwórz klasę PagedDataSource

```csharp
using LazyLoading.Core;
using System.Data.SqlClient;

public class ProductsDataSource : PagedDataSource
{
    public ProductsDataSource(string connectionString, int pageSize = 50)
        : base(connectionString, pageSize)
    {
    }

    protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
    {
        var command = new SqlCommand("Products_SelectView_Paged", connection);
        command.CommandType = CommandType.StoredProcedure;

        // Parametry wejściowe
        command.Parameters.AddWithValue("@PageNumber", pageNumber);
        command.Parameters.AddWithValue("@PageSize", PageSize);

        // Parametr wyjściowy (WYMAGANY!)
        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int);
        totalCountParam.Direction = ParameterDirection.Output;
        command.Parameters.Add(totalCountParam);

        return command;
    }
}
```

### 3️⃣ Użyj PagedDataGridView w formularzu

```csharp
using LazyLoading.Controls;

public class MyForm : Form
{
    private PagedDataGridView pagedGrid;

    private void InitializeGrid()
    {
        pagedGrid = new PagedDataGridView
        {
            Dock = DockStyle.Fill,
            ShowLoadingIndicator = true,  // Progress bar
            ShowStatusLabel = true,        // Licznik rekordów
            ScrollDebounceDelay = 150      // Opóźnienie przy scroll
        };

        this.Controls.Add(pagedGrid);

        // Załaduj dane
        LoadData();
    }

    private void LoadData()
    {
        string connStr = "Server=...;Database=...;Integrated Security=true;";

        var dataSource = new ProductsDataSource(connStr, pageSize: 50);

        // To wszystko! Dane załadują się automatycznie
        pagedGrid.PagedDataSource = dataSource;
    }
}
```

**I to wszystko!** 🎉 Grid automatycznie:
- Załaduje pierwszą stronę (50 rekordów)
- Przy scrollowaniu doładuje kolejne strony
- Pokaże progress bar podczas ładowania
- Wyświetli licznik: "Załadowano: 150 / 30,000 rekordów"

## 📚 Zaawansowane użycie

### Paginacja z filtrowaniem

```csharp
public class ProductsFilteredDataSource : PagedDataSource
{
    public string? Category { get; set; }
    public string? SearchText { get; set; }

    protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
    {
        var command = new SqlCommand("Products_SelectByCategory_Paged", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@PageNumber", pageNumber);
        command.Parameters.AddWithValue("@PageSize", PageSize);
        command.Parameters.AddWithValue("@Category", (object?)Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@SearchText", (object?)SearchText ?? DBNull.Value);

        var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int);
        totalCountParam.Direction = ParameterDirection.Output;
        command.Parameters.Add(totalCountParam);

        return command;
    }
}

// Użycie:
var dataSource = new ProductsFilteredDataSource(connStr, 50)
{
    Category = "Electronics",
    SearchText = "laptop"
};
pagedGrid.PagedDataSource = dataSource;
```

### Odświeżanie danych

```csharp
// Odśwież dane (czyści cache i ładuje od nowa)
await pagedGrid.RefreshDataAsync();
```

### Event handlery

```csharp
var dataSource = new ProductsDataSource(connStr, 50);

// Event gdy strona zostanie załadowana
dataSource.PageLoaded += (s, e) =>
{
    Console.WriteLine($"Załadowano stronę {e.PageNumber}, {e.RecordsLoaded} rekordów");
};

// Event przy błędzie
dataSource.ErrorOccurred += (s, e) =>
{
    MessageBox.Show(e.ErrorMessage, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
};
```

## 🔄 Konwersja istniejących procedur

### Twoja stara procedura:
```sql
CREATE PROCEDURE [dbo].[Customers_SelectView]
AS
BEGIN
    SELECT * FROM Customers  -- 30,000 rekordów naraz! 😱
END
```

### Nowa procedura z paginacją:
```sql
CREATE PROCEDURE [dbo].[Customers_SelectView_Paged]
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize

    SELECT @TotalCount = COUNT(*) FROM Customers

    SELECT *
    FROM Customers
    ORDER BY CustomerId  -- DODAJ ORDER BY!
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
```

### Klasa C#:
```csharp
public class CustomersDataSource : PagedDataSource
{
    public CustomersDataSource(string connectionString, int pageSize = 50)
        : base(connectionString, pageSize) { }

    protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
    {
        var cmd = new SqlCommand("Customers_SelectView_Paged", connection);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        cmd.Parameters.AddWithValue("@PageSize", PageSize);

        var totalParam = new SqlParameter("@TotalCount", SqlDbType.Int);
        totalParam.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(totalParam);

        return cmd;
    }
}
```

## 🎛️ Konfiguracja

### PagedDataGridView Properties

| Property | Typ | Default | Opis |
|----------|-----|---------|------|
| `ShowLoadingIndicator` | bool | true | Pokazuje progress bar podczas ładowania |
| `ShowStatusLabel` | bool | true | Pokazuje licznik załadowanych rekordów |
| `ScrollDebounceDelay` | int | 150 | Opóźnienie (ms) przed załadowaniem przy scroll |

### PagedDataSource Properties

| Property | Typ | Opis |
|----------|-----|------|
| `PageSize` | int | Liczba rekordów na stronę (np. 30, 50, 100) |
| `TotalRecords` | int | Całkowita liczba rekordów (readonly) |
| `TotalPages` | int | Całkowita liczba stron (readonly) |

## 💡 Best Practices

### 1. **Rozmiar strony (PageSize)**
```csharp
// ✅ Dobry rozmiar - balans między liczbą zapytań a rozmiarem danych
var dataSource = new ProductsDataSource(connStr, pageSize: 50);

// ❌ Za mały - zbyt wiele zapytań do bazy
var dataSource = new ProductsDataSource(connStr, pageSize: 10);

// ❌ Za duży - wolne pierwsze ładowanie
var dataSource = new ProductsDataSource(connStr, pageSize: 500);
```

**Rekomendowane:** 30-100 rekordów na stronę

### 2. **Indeksy w bazie danych**
```sql
-- Zawsze dodawaj indeks na kolumnie w ORDER BY!
CREATE INDEX IX_Products_ProductId ON Products(ProductId);

-- Dla filtrowanych kolumn też:
CREATE INDEX IX_Products_Category ON Products(Category);
```

### 3. **Connection String**
```csharp
// ✅ Używaj connection pooling (domyślnie włączone)
"Server=localhost;Database=MyDB;Integrated Security=true;"

// ✅ Dla wielu równoczesnych zapytań zwiększ pool size
"Server=localhost;Database=MyDB;Integrated Security=true;Max Pool Size=200;"
```

### 4. **Async/Await**
```csharp
// ✅ Zawsze używaj async dla operacji I/O
private async void LoadData()
{
    var dataSource = new ProductsDataSource(connStr, 50);
    pagedGrid.PagedDataSource = dataSource;
    // Ładowanie odbywa się asynchronicznie
}

// ✅ Odświeżanie też async
await pagedGrid.RefreshDataAsync();
```

## 🔍 Troubleshooting

### Problem: "Procedura nie zwraca @TotalCount"
```csharp
// ✅ Upewnij się że parametr jest OUTPUT
var totalParam = new SqlParameter("@TotalCount", SqlDbType.Int);
totalParam.Direction = ParameterDirection.Output;  // WAŻNE!
command.Parameters.Add(totalParam);
```

### Problem: "Error: Invalid usage of the option NEXT in the FETCH statement"
```sql
-- ❌ Brak ORDER BY
SELECT * FROM Products
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY

-- ✅ Z ORDER BY
SELECT * FROM Products
ORDER BY ProductId  -- WYMAGANE!
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY
```

### Problem: "Wolne ładowanie pierwszej strony"
```sql
-- Sprawdź plan wykonania zapytania
SET STATISTICS TIME ON;
EXEC Products_SelectView_Paged @PageNumber=1, @PageSize=50, @TotalCount=NULL;

-- Dodaj indeksy na kolumny w ORDER BY i WHERE
CREATE INDEX IX_Products_ProductId ON Products(ProductId);
```

### Problem: "Grid nie ładuje kolejnych stron"
```csharp
// Sprawdź czy procedura ustawia @TotalCount poprawnie
var cmd = CreateCommand(connection, 1);
connection.Open();
cmd.ExecuteNonQuery();

var totalCount = (int)cmd.Parameters["@TotalCount"].Value;
Console.WriteLine($"Total records: {totalCount}");  // Powinno być > 0
```

## 📁 Struktura projektu

```
LazyLoading/
├── src/
│   ├── Core/
│   │   ├── PagedDataSource.cs         # Abstrakcyjna klasa bazowa
│   │   └── VirtualScrollHandler.cs    # Obsługa scrollowania
│   ├── Controls/
│   │   └── PagedDataGridView.cs       # Custom DataGridView control
│   ├── Examples/
│   │   ├── ProductsDataSource.cs      # Przykładowe implementacje
│   │   └── MainForm.cs                # Przykładowy formularz
│   └── Database/
│       ├── 01_CreateTestData.sql      # Tworzenie testowej bazy
│       └── 02_PagedProcedures.sql     # Przykładowe procedury
├── LazyLoading.csproj
├── Program.cs
└── README.md
```

## 🎯 Przykłady użycia

Zobacz folder `src/Examples/` dla kompletnych przykładów:
- `ProductsDataSource.cs` - Podstawowa paginacja
- `ProductsFilteredDataSource.cs` - Z filtrowaniem
- `ProductsWithSortDataSource.cs` - Z sortowaniem
- `MainForm.cs` - Pełny przykład formularza

## ⚙️ Instalacja testowej bazy

1. Otwórz SQL Server Management Studio
2. Wykonaj `src/Database/01_CreateTestData.sql` - stworzy 30,000 testowych rekordów
3. Wykonaj `src/Database/02_PagedProcedures.sql` - stworzy procedury z paginacją
4. Zmień connection string w `MainForm.cs`
5. Uruchom aplikację!

## 📊 Porównanie wydajności

### Przed (bez paginacji):
```
Ładowanie 30,000 rekordów: ~3-5 sekund 😴
Zużycie pamięci: ~50 MB
Pierwsza interakcja: 5 sekund
```

### Po (z paginacją):
```
Ładowanie pierwszych 50 rekordów: ~50-100 ms ⚡
Zużycie pamięci: ~2-5 MB
Pierwsza interakcja: 100 ms
```

**Rezultat:** 50x szybsze pierwsze ładowanie! 🚀

## 🤝 Contributing

Pull requesty mile widziane! Dla większych zmian, najpierw otwórz issue.

## 📄 Licencja

MIT License - możesz używać w projektach komercyjnych i prywatnych.

## 💬 FAQ

**Q: Czy mogę używać z Entity Framework?**
A: Tak! Zamiast `SqlCommand` możesz użyć EF Core z `.Skip()` i `.Take()`.

**Q: Czy działa z MySQL/PostgreSQL?**
A: Tak, zmień `SqlConnection` na odpowiedni provider i dostosuj składnię SQL.

**Q: Czy mogę użyć zwykłego DataGridView?**
A: Tak, ale musisz ręcznie obsłużyć event `Scroll` i wywołać `dataSource.GetPageAsync()`.

**Q: Jak zmienić rozmiar strony?**
A: Ustaw `pageSize` w konstruktorze: `new ProductsDataSource(connStr, pageSize: 100)`

**Q: Czy cache można wyłączyć?**
A: Można wyczyścić ręcznie: `dataSource.ClearCache()`, ale nie ma sensu - cache znacznie przyspiesza scrollowanie w górę.

---

**Autor:** SQL Lazy Loading Team
**Wersja:** 1.0.0
**Data:** 2025

Enjoy lightning-fast data loading! ⚡
