# ⚡ Quick Start - 5 minut do działającej paginacji!

## Krok 1: Przygotuj bazę danych (2 min)

### A. Wykonaj skrypty SQL

```sql
-- 1. Otwórz SQL Server Management Studio
-- 2. Połącz się z bazą danych
-- 3. Zmień nazwę bazy w plikach SQL
-- 4. Wykonaj kolejno:

-- Plik: src/Database/01_CreateTestData.sql
-- Tworzy tabelę Products i 30,000 testowych rekordów

-- Plik: src/Database/02_PagedProcedures.sql
-- Tworzy procedury z paginacją
```

### B. Lub użyj własnej procedury

Minimalny szablon procedury z paginacją:

```sql
CREATE PROCEDURE YourTable_SelectView_Paged
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @TotalCount INT OUTPUT
AS
BEGIN
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize

    -- Total count
    SELECT @TotalCount = COUNT(*) FROM YourTable

    -- Data page
    SELECT * FROM YourTable
    ORDER BY Id  -- WAŻNE: ORDER BY jest wymagane!
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
```

## Krok 2: Zmień connection string (30 sek)

W pliku `src/Examples/MainForm.cs` linia 15:

```csharp
// PRZED:
private const string CONNECTION_STRING = "Server=localhost;Database=YourDatabaseName;Integrated Security=true;";

// PO (twoje dane):
private const string CONNECTION_STRING = "Server=MojSerwer;Database=MojaBaza;Integrated Security=true;";
```

## Krok 3: Uruchom aplikację (1 min)

### Opcja A: Visual Studio
```bash
1. Otwórz LazyLoading.csproj w Visual Studio
2. Naciśnij F5 lub "Start"
```

### Opcja B: Wiersz poleceń
```bash
cd /path/to/LazyLoading
dotnet restore
dotnet run
```

### Opcja C: Publikacja
```bash
dotnet publish -c Release -r win-x64 --self-contained
# Executable w: bin/Release/net6.0-windows/win-x64/publish/
```

## Krok 4: Integracja z twoim projektem (2 min)

### Skopiuj pliki do swojego projektu:

```
Twój projekt/
├── Core/
│   ├── PagedDataSource.cs           ← SKOPIUJ
│   ├── VirtualScrollHandler.cs      ← SKOPIUJ
│   ├── GenericPagedDataSource.cs    ← SKOPIUJ (opcjonalnie)
│   └── DataGridViewExtensions.cs    ← SKOPIUJ (opcjonalnie)
└── Controls/
    └── PagedDataGridView.cs         ← SKOPIUJ
```

### Dodaj do .csproj:

```xml
<PackageReference Include="System.Data.SqlClient" Version="4.8.5" />
```

## Krok 5: Użyj w swoim formularzu (1 min)

### Metoda 1: GenericPagedDataSource (najszybsze)

```csharp
using LazyLoading.Core;
using LazyLoading.Controls;

public class MyForm : Form
{
    private void LoadGrid()
    {
        var grid = new PagedDataGridView { Dock = DockStyle.Fill };
        this.Controls.Add(grid);

        var dataSource = new GenericPagedDataSource(
            connectionString: "Server=...;Database=...;Integrated Security=true;",
            storedProcedureName: "YourTable_SelectView_Paged",
            pageSize: 50
        );

        grid.PagedDataSource = dataSource;
        // DONE! 🎉
    }
}
```

### Metoda 2: Własna klasa (więcej kontroli)

```csharp
// 1. Stwórz klasę:
public class YourDataSource : PagedDataSource
{
    public YourDataSource(string connStr, int pageSize = 50)
        : base(connStr, pageSize) { }

    protected override SqlCommand CreateCommand(SqlConnection conn, int pageNum)
    {
        var cmd = new SqlCommand("YourTable_SelectView_Paged", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@PageNumber", pageNum);
        cmd.Parameters.AddWithValue("@PageSize", PageSize);

        var total = new SqlParameter("@TotalCount", SqlDbType.Int);
        total.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(total);

        return cmd;
    }
}

// 2. Użyj:
var grid = new PagedDataGridView { Dock = DockStyle.Fill };
grid.PagedDataSource = new YourDataSource("connection_string", 50);
```

## ✅ Checklist - Czy działa?

- [ ] Baza danych działa (sprawdź connection string)
- [ ] Procedura ma parametry `@PageNumber`, `@PageSize`, `@TotalCount OUTPUT`
- [ ] Procedura ma `ORDER BY` (wymagane dla `OFFSET/FETCH`)
- [ ] W projekcie jest `System.Data.SqlClient` package
- [ ] Skopiowano wszystkie wymagane pliki (`PagedDataSource.cs`, `VirtualScrollHandler.cs`, `PagedDataGridView.cs`)

## 🐛 Nie działa? Sprawdź:

### Error: "Invalid object name 'Products'"
```
❌ Baza danych nie ma tabeli/procedury
✅ Wykonaj skrypty SQL lub zmień nazwę procedury w kodzie
```

### Error: "Login failed for user"
```
❌ Błędny connection string
✅ Sprawdź serwer, bazę, credentials
```

### Error: "Could not find stored procedure"
```
❌ Procedura nie istnieje lub źle nazwana
✅ Sprawdź nazwę procedury w SSMS i kodzie C#
```

### Error: "Invalid usage of NEXT in FETCH"
```
❌ Brak ORDER BY w procedurze
✅ Dodaj ORDER BY przed OFFSET/FETCH
```

### Grid jest pusty (bez błędów)
```
❌ Procedura zwraca 0 rekordów lub @TotalCount = 0
✅ Sprawdź:
   - SELECT @TotalCount = COUNT(*) FROM YourTable
   - Czy tabela ma dane?
   - Uruchom procedurę ręcznie w SSMS
```

## 📞 Potrzebujesz pomocy?

1. Sprawdź pełną dokumentację w `README.md`
2. Zobacz przykłady w folderze `src/Examples/`
3. Sprawdź FAQ w `README.md`

## 🎯 Następne kroki

Po uruchomieniu podstawowej wersji:

1. **Dodaj filtry** - zobacz `ProductsByCategoryDataSource.cs`
2. **Dodaj sortowanie** - zobacz `ProductsWithSortDataSource.cs`
3. **Optymalizuj SQL** - dodaj indeksy na kolumny w ORDER BY
4. **Dostosuj UI** - zmień kolory, czcionki, layouty
5. **Zmień PageSize** - eksperymentuj z 30, 50, 100 rekordami

---

**Gratulacje!** 🎉 Masz działającą paginację SQL w WinForms!

**Czas realizacji:** ~5 minut
**Przyrost wydajności:** ~50x szybsze pierwsze ładowanie

Miłego kodowania! ⚡
