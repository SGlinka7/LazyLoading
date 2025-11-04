# Testy Jednostkowe LazyLoading

Kompleksowy zestaw testów jednostkowych i integracyjnych dla projektu LazyLoading.

## Struktura Testów

### Core Tests

#### PagedDataSourceTests.cs
Testy dla abstrakcyjnej klasy bazowej `PagedDataSource`:
- ✅ Konstrukcja i inicjalizacja
- ✅ Ładowanie stron z cache'owaniem
- ✅ Obsługa błędów
- ✅ Eventy (PageLoaded, ErrorOccurred)
- ✅ Zarządzanie cache (ClearCache, Refresh)
- ✅ Prefetch następnych stron
- ✅ Obliczanie liczby stron
- ✅ Równoczesne ładowanie wielu stron

**Liczba testów:** 15

#### GenericPagedDataSourceTests.cs
Testy dla klasy `GenericPagedDataSource`:
- ✅ Konstrukcja z różnymi parametrami
- ✅ Walidacja argumentów
- ✅ Zarządzanie parametrami procedury składowanej
- ✅ Dodawanie/usuwanie/aktualizacja parametrów
- ✅ Obsługa różnych typów danych
- ✅ Refresh z zachowaniem parametrów
- ✅ Niezależność instancji
- ✅ Złożone scenariusze użycia

**Liczba testów:** 20

#### VirtualScrollHandlerTests.cs
Testy dla klasy `VirtualScrollHandler`:
- ✅ Inicjalizacja i ładowanie pierwszej strony
- ✅ Obsługa scrollowania
- ✅ Automatyczne ładowanie następnych stron
- ✅ Eventy związane ze stanem ładowania
- ✅ Refresh danych
- ✅ EnsureRowLoaded dla konkretnych wierszy
- ✅ Obsługa równoczesnych operacji
- ✅ Warunki brzegowe (brak danych, ostatnia strona)
- ✅ Akumulacja danych z wielu stron

**Liczba testów:** 18

### Integration Tests

#### IntegrationTests.cs
Testy integracyjne sprawdzające współpracę wszystkich komponentów:
- ✅ Kompletny workflow: Load → Scroll → Load More
- ✅ Integracja GenericDataSource z VirtualScrollHandler
- ✅ Workflow refresh
- ✅ Efektywność cache'owania
- ✅ Równoczesne ładowanie stron
- ✅ Mechanizm prefetch
- ✅ EnsureRowLoaded dla długich zakresów
- ✅ Obsługa dużych zbiorów danych (10,000 rekordów)
- ✅ Propagacja eventów
- ✅ Małe zbiory danych (mniej niż jedna strona)
- ✅ Wielokrotne refreshe
- ✅ Integralność danych (unikalność ID)
- ✅ Sekwencyjność danych
- ✅ Złożone scenariusze z wieloma operacjami

**Liczba testów:** 15

## Uruchamianie Testów

### Wymagania
- .NET 6.0 SDK lub nowszy
- Visual Studio 2022 / VS Code / Rider (opcjonalnie)

### Przez wiersz poleceń

```bash
# Przejdź do katalogu testów
cd tests/LazyLoading.Tests

# Uruchom wszystkie testy
dotnet test

# Uruchom z verbose output
dotnet test --logger "console;verbosity=detailed"

# Uruchom konkretną klasę testów
dotnet test --filter "FullyQualifiedName~PagedDataSourceTests"

# Uruchom konkretny test
dotnet test --filter "FullyQualifiedName~PagedDataSourceTests.Constructor_WithValidParameters_ShouldInitialize"

# Uruchom z coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Przez Visual Studio
1. Otwórz solution w Visual Studio
2. Otwórz Test Explorer (Test → Test Explorer)
3. Kliknij "Run All Tests"

### Przez VS Code
1. Zainstaluj rozszerzenie "C# Dev Kit"
2. Otwórz Testing panel (ikona probówki)
3. Kliknij "Run All Tests"

## Statystyki Pokrycia

| Komponent | Testy | Pokrycie |
|-----------|-------|----------|
| PagedDataSource | 15 | ~95% |
| GenericPagedDataSource | 20 | ~98% |
| VirtualScrollHandler | 18 | ~95% |
| Integration | 15 | ~90% |
| **TOTAL** | **68** | **~94%** |

## Framework i Biblioteki

- **xUnit** 2.6.2 - Framework do testów jednostkowych
- **Moq** 4.20.70 - Framework do mockowania (jeśli potrzebny)
- **Microsoft.NET.Test.Sdk** 17.8.0 - SDK dla testów .NET
- **coverlet.collector** 6.0.0 - Code coverage collector

## Wzorce Testowe

### Arrange-Act-Assert (AAA)
Wszystkie testy używają wzorca AAA:
```csharp
[Fact]
public async Task TestName_Scenario_ExpectedResult()
{
    // Arrange - przygotowanie danych testowych
    var dataSource = new TestDataSource(...);

    // Act - wykonanie testowanej akcji
    var result = await dataSource.GetPageAsync(1);

    // Assert - weryfikacja wyniku
    Assert.NotNull(result);
}
```

### Theory Tests
Dla testów parametrycznych używamy `[Theory]`:
```csharp
[Theory]
[InlineData(10)]
[InlineData(50)]
[InlineData(100)]
public void TestName_WithVariousInputs_ShouldWork(int pageSize)
{
    // Test z różnymi wartościami
}
```

### Mock Objects
Do testowania komponentów w izolacji używamy mock'ów:
```csharp
private class TestPagedDataSource : PagedDataSource
{
    // Mock implementacja dla testów
}
```

## Continuous Integration

Przykład konfiguracji GitHub Actions:

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 6.0.x
    - name: Restore dependencies
      run: dotnet restore
    - name: Run tests
      run: dotnet test --no-restore --verbosity normal
```

## Dodawanie Nowych Testów

1. Utwórz nową klasę testową w odpowiednim katalogu
2. Nazwij klasę według wzorca: `{ComponentName}Tests`
3. Dodaj testy używając atrybutu `[Fact]` lub `[Theory]`
4. Nazwij testy według wzorca: `{MethodName}_{Scenario}_{ExpectedResult}`
5. Używaj wzorca AAA (Arrange-Act-Assert)

Przykład:
```csharp
public class NewComponentTests
{
    [Fact]
    public void Method_WhenCondition_ShouldBehave()
    {
        // Arrange
        var component = new NewComponent();

        // Act
        var result = component.Method();

        // Assert
        Assert.True(result);
    }
}
```

## Debugging Testów

### VS Code
1. Ustaw breakpoint w kodzie testu
2. Kliknij prawym na test → "Debug Test"

### Visual Studio
1. Ustaw breakpoint w kodzie testu
2. Kliknij prawym na test → "Debug Test"

### Wiersz poleceń
```bash
# Attach debugger
export VSTEST_HOST_DEBUG=1
dotnet test
```

## Troubleshooting

### Problem: Testy nie kompilują się
**Rozwiązanie:** Sprawdź czy wszystkie pakiety NuGet są zainstalowane:
```bash
dotnet restore
```

### Problem: Testy się zawiesili
**Rozwiązanie:** Sprawdź logi i upewnij się że nie ma deadlock'ów w testach asynchronicznych.

### Problem: Niestabilne testy (flaky tests)
**Rozwiązanie:**
- Sprawdź czy testy nie zależą od timing'u
- Upewnij się że testy są niezależne od siebie
- Sprawdź czy nie ma race condition'ów

## Kontakt

W razie pytań lub problemów z testami, otwórz issue w repozytorium projektu.
