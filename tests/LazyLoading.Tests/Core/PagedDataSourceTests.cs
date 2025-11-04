using System.Data;
using System.Data.SqlClient;
using LazyLoading.Core;
using Xunit;

namespace LazyLoading.Tests.Core
{
    /// <summary>
    /// Testy jednostkowe dla klasy PagedDataSource.
    /// </summary>
    public class PagedDataSourceTests
    {
        // Testowa implementacja PagedDataSource
        private class TestPagedDataSource : PagedDataSource
        {
            private readonly Func<int, DataTable?>? _pageProvider;
            private readonly Action<SqlConnection, int>? _commandSetup;

            public TestPagedDataSource(
                string connectionString,
                int pageSize = 50,
                Func<int, DataTable?>? pageProvider = null,
                Action<SqlConnection, int>? commandSetup = null)
                : base(connectionString, pageSize)
            {
                _pageProvider = pageProvider;
                _commandSetup = commandSetup;
            }

            protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
            {
                var command = new SqlCommand("TestProc", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@PageNumber", pageNumber);
                command.Parameters.AddWithValue("@PageSize", PageSize);

                var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int);
                totalCountParam.Direction = ParameterDirection.Output;
                totalCountParam.Value = 100; // Mock value
                command.Parameters.Add(totalCountParam);

                _commandSetup?.Invoke(connection, pageNumber);

                return command;
            }

            protected override async Task<DataTable?> LoadPageFromDatabaseAsync(int pageNumber)
            {
                if (_pageProvider != null)
                {
                    var result = _pageProvider(pageNumber);
                    if (result != null && result.Rows.Count > 0)
                    {
                        // Symuluj ustawienie TotalRecords
                        typeof(PagedDataSource)
                            .GetProperty("TotalRecords")!
                            .SetValue(this, 100);
                    }
                    return result;
                }

                return await base.LoadPageFromDatabaseAsync(pageNumber);
            }
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            var dataSource = new TestPagedDataSource("Server=test;Database=test", 50);

            // Assert
            Assert.Equal(50, dataSource.PageSize);
            Assert.Equal(0, dataSource.TotalRecords);
            Assert.Equal(0, dataSource.TotalPages);
        }

        [Fact]
        public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestPagedDataSource(null!));
        }

        [Fact]
        public async Task GetPageAsync_WithValidPageNumber_ShouldReturnData()
        {
            // Arrange
            var testData = CreateTestDataTable(10);
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber => testData);

            // Act
            var result = await dataSource.GetPageAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Rows.Count);
        }

        [Fact]
        public async Task GetPageAsync_WithInvalidPageNumber_ShouldThrowException()
        {
            // Arrange
            var dataSource = new TestPagedDataSource("Server=test;Database=test");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                dataSource.GetPageAsync(0));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                dataSource.GetPageAsync(-1));
        }

        [Fact]
        public async Task GetPageAsync_CalledTwiceWithSamePageNumber_ShouldUseCacheSecondTime()
        {
            // Arrange
            int loadCount = 0;
            var testData = CreateTestDataTable(10);
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber =>
                {
                    loadCount++;
                    return testData;
                });

            // Act
            var result1 = await dataSource.GetPageAsync(1);
            var result2 = await dataSource.GetPageAsync(1);

            // Assert
            Assert.Equal(1, loadCount); // Załadowano tylko raz
            Assert.Same(result1, result2); // Zwrócono tę samą referencję z cache
        }

        [Fact]
        public async Task GetPageAsync_ShouldRaisePageLoadedEvent()
        {
            // Arrange
            var testData = CreateTestDataTable(10);
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber => testData);

            PageLoadedEventArgs? eventArgs = null;
            dataSource.PageLoaded += (sender, e) => eventArgs = e;

            // Act
            await dataSource.GetPageAsync(1);

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(1, eventArgs.PageNumber);
            Assert.Equal(10, eventArgs.RecordsLoaded);
        }

        [Fact]
        public async Task GetPageAsync_WithError_ShouldRaiseErrorOccurredEvent()
        {
            // Arrange
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber => throw new Exception("Test error"));

            ErrorEventArgs? eventArgs = null;
            dataSource.ErrorOccurred += (sender, e) => eventArgs = e;

            // Act
            var result = await dataSource.GetPageAsync(1);

            // Assert
            Assert.Null(result);
            Assert.NotNull(eventArgs);
            Assert.Contains("Test error", eventArgs.ErrorMessage);
        }

        [Fact]
        public async Task GetPagesAsync_WithMultiplePageNumbers_ShouldReturnAllPages()
        {
            // Arrange
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber => CreateTestDataTable(10, pageNumber));

            // Act
            var results = await dataSource.GetPagesAsync(1, 2, 3);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.True(results.ContainsKey(1));
            Assert.True(results.ContainsKey(2));
            Assert.True(results.ContainsKey(3));
        }

        [Fact]
        public void TotalPages_ShouldCalculateCorrectly()
        {
            // Arrange
            var dataSource = new TestPagedDataSource("Server=test;Database=test", 50);

            // Symuluj ustawienie TotalRecords
            typeof(PagedDataSource)
                .GetProperty("TotalRecords")!
                .SetValue(dataSource, 125);

            // Act
            var totalPages = dataSource.TotalPages;

            // Assert
            Assert.Equal(3, totalPages); // 125 / 50 = 2.5 => 3 strony
        }

        [Fact]
        public void ClearCache_ShouldRemoveAllCachedPages()
        {
            // Arrange
            int loadCount = 0;
            var testData = CreateTestDataTable(10);
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber =>
                {
                    loadCount++;
                    return testData;
                });

            // Act
            var result1 = dataSource.GetPageAsync(1).Result;
            dataSource.ClearCache();
            var result2 = dataSource.GetPageAsync(1).Result;

            // Assert
            Assert.Equal(2, loadCount); // Załadowano dwa razy (cache został wyczyszczony)
        }

        [Fact]
        public void Refresh_ShouldClearCacheAndResetTotalRecords()
        {
            // Arrange
            var testData = CreateTestDataTable(10);
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber => testData);

            // Załaduj stronę i ustaw TotalRecords
            dataSource.GetPageAsync(1).Wait();

            // Act
            dataSource.Refresh();

            // Assert
            Assert.Equal(0, dataSource.TotalRecords);
        }

        [Fact]
        public async Task PrefetchNextPagesAsync_ShouldLoadPagesInBackground()
        {
            // Arrange
            var loadedPages = new List<int>();
            var dataSource = new TestPagedDataSource(
                "Server=test;Database=test",
                50,
                pageNumber =>
                {
                    loadedPages.Add(pageNumber);
                    return CreateTestDataTable(10, pageNumber);
                });

            // Ustaw TotalRecords i TotalPages
            typeof(PagedDataSource)
                .GetProperty("TotalRecords")!
                .SetValue(dataSource, 250); // 5 stron

            // Act
            await dataSource.PrefetchNextPagesAsync(1, 2);
            await Task.Delay(500); // Daj czas na załadowanie w tle

            // Assert
            Assert.Contains(2, loadedPages);
            Assert.Contains(3, loadedPages);
        }

        [Fact]
        public void PageSize_CanBeChanged()
        {
            // Arrange
            var dataSource = new TestPagedDataSource("Server=test;Database=test", 50);

            // Act
            dataSource.PageSize = 100;

            // Assert
            Assert.Equal(100, dataSource.PageSize);
        }

        // Helper methods
        private DataTable CreateTestDataTable(int rowCount, int pageId = 1)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Value", typeof(decimal));

            for (int i = 0; i < rowCount; i++)
            {
                var row = table.NewRow();
                row["Id"] = (pageId - 1) * rowCount + i + 1;
                row["Name"] = $"Item {row["Id"]}";
                row["Value"] = (decimal)(i * 10.5);
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
