using System.Data;
using System.Data.SqlClient;
using LazyLoading.Core;
using Xunit;

namespace LazyLoading.Tests.Integration
{
    /// <summary>
    /// Testy integracyjne dla całego systemu lazy loading.
    /// </summary>
    public class IntegrationTests
    {
        // Mock implementacja dla testów integracyjnych
        private class TestDataSource : PagedDataSource
        {
            private readonly Dictionary<int, DataTable> _mockData;

            public TestDataSource(string connectionString, int pageSize, int totalRecords)
                : base(connectionString, pageSize)
            {
                _mockData = new Dictionary<int, DataTable>();
                PrepareMockData(totalRecords, pageSize);

                // Ustaw TotalRecords
                typeof(PagedDataSource)
                    .GetProperty("TotalRecords")!
                    .SetValue(this, totalRecords);
            }

            private void PrepareMockData(int totalRecords, int pageSize)
            {
                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                for (int page = 1; page <= totalPages; page++)
                {
                    int rowsInPage = Math.Min(pageSize, totalRecords - (page - 1) * pageSize);
                    _mockData[page] = CreatePageData(page, rowsInPage, pageSize);
                }
            }

            private DataTable CreatePageData(int pageNumber, int rowCount, int pageSize)
            {
                var table = new DataTable();
                table.Columns.Add("Id", typeof(int));
                table.Columns.Add("ProductName", typeof(string));
                table.Columns.Add("Price", typeof(decimal));
                table.Columns.Add("InStock", typeof(bool));

                int startId = (pageNumber - 1) * pageSize + 1;

                for (int i = 0; i < rowCount; i++)
                {
                    var row = table.NewRow();
                    row["Id"] = startId + i;
                    row["ProductName"] = $"Product {startId + i}";
                    row["Price"] = (decimal)(10.5 * (startId + i));
                    row["InStock"] = (startId + i) % 2 == 0;
                    table.Rows.Add(row);
                }

                return table;
            }

            protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
            {
                throw new NotImplementedException();
            }

            protected override async Task<DataTable?> LoadPageFromDatabaseAsync(int pageNumber)
            {
                await Task.Delay(50); // Symuluj opóźnienie sieciowe

                if (_mockData.TryGetValue(pageNumber, out var data))
                {
                    return data;
                }

                return null;
            }
        }

        [Fact]
        public async Task CompleteWorkflow_LoadAndScroll_ShouldWorkCorrectly()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 250);
            var handler = new VirtualScrollHandler(dataSource);

            // Act & Assert - Initialize
            await handler.InitializeAsync();
            Assert.Equal(50, handler.GetDataTable().Rows.Count);

            // Act & Assert - Scroll to load more
            await handler.HandleScrollAsync(35, 20);
            Assert.Equal(100, handler.GetDataTable().Rows.Count);

            // Act & Assert - Continue scrolling
            await handler.HandleScrollAsync(85, 20);
            Assert.Equal(150, handler.GetDataTable().Rows.Count);
        }

        [Fact]
        public async Task GenericDataSource_WithVirtualScrollHandler_ShouldIntegrate()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 30, 150);
            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.InitializeAsync();
            await handler.HandleScrollAsync(20, 15);

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.True(dataTable.Rows.Count >= 60); // At least 2 pages
            Assert.Equal(30, handler.RowsPerPage);
            Assert.Equal(150, handler.TotalRows);
        }

        [Fact]
        public async Task RefreshWorkflow_ShouldResetAndReload()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 200);
            var handler = new VirtualScrollHandler(dataSource);

            await handler.InitializeAsync();
            await handler.HandleScrollAsync(35, 20);
            await handler.HandleScrollAsync(85, 20);

            var rowCountBeforeRefresh = handler.GetDataTable().Rows.Count;
            Assert.Equal(150, rowCountBeforeRefresh);

            // Act - Refresh
            await handler.RefreshAsync();

            // Assert
            var rowCountAfterRefresh = handler.GetDataTable().Rows.Count;
            Assert.Equal(50, rowCountAfterRefresh); // Back to first page only
        }

        [Fact]
        public async Task CacheEfficiency_ShouldNotReloadSamePage()
        {
            // Arrange
            int loadCount = 0;
            var dataSource = new TestDataSource("test", 50, 150);

            // Liczymy ile razy ładujemy stronę
            dataSource.PageLoaded += (sender, e) => loadCount++;

            // Act
            await dataSource.GetPageAsync(1);
            await dataSource.GetPageAsync(1); // Druga próba - powinna być z cache
            await dataSource.GetPageAsync(2);
            await dataSource.GetPageAsync(2); // Druga próba - powinna być z cache

            // Assert
            Assert.Equal(2, loadCount); // Tylko 2 unikalne strony załadowane
        }

        [Fact]
        public async Task ConcurrentPageLoads_ShouldHandleCorrectly()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 300);

            // Act - Load multiple pages concurrently
            var task1 = dataSource.GetPageAsync(1);
            var task2 = dataSource.GetPageAsync(2);
            var task3 = dataSource.GetPageAsync(3);
            var task4 = dataSource.GetPageAsync(4);

            var results = await Task.WhenAll(task1, task2, task3, task4);

            // Assert
            Assert.Equal(4, results.Length);
            Assert.All(results, page => Assert.NotNull(page));
            Assert.All(results, page => Assert.Equal(50, page!.Rows.Count));
        }

        [Fact]
        public async Task PrefetchMechanism_ShouldLoadNextPagesInBackground()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 300);
            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.InitializeAsync();
            await Task.Delay(200); // Daj czas na prefetch

            // Sprawdź czy prefetch zadziałał (strony 2 i 3 powinny być w cache)
            var loadedPages = new List<int>();
            dataSource.PageLoaded += (sender, e) => loadedPages.Add(e.PageNumber);

            await dataSource.GetPageAsync(2);
            await dataSource.GetPageAsync(3);

            // Assert
            // Jeśli strony zostały prefetchowane, nie będzie nowych wywołań PageLoaded
            Assert.Empty(loadedPages);
        }

        [Fact]
        public async Task EnsureRowLoaded_ShouldLoadAllRequiredPages()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 40, 200);
            var handler = new VirtualScrollHandler(dataSource);

            await handler.InitializeAsync();

            // Act - Ensure row 150 is loaded (requires pages 1-4)
            await handler.EnsureRowLoadedAsync(150);

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.True(dataTable.Rows.Count >= 151);
        }

        [Fact]
        public async Task LargeDataSet_ShouldHandleEfficiently()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 100, 10000);
            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.InitializeAsync();

            // Assert - Only first page loaded initially
            Assert.Equal(100, handler.GetDataTable().Rows.Count);
            Assert.Equal(10000, handler.TotalRows);
            Assert.Equal(100, dataSource.TotalPages);
        }

        [Fact]
        public async Task EventHandling_ShouldPropagateCorrectly()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 200);
            var handler = new VirtualScrollHandler(dataSource);

            var pageLoadedEvents = new List<PageLoadedEventArgs>();
            var loadingStateChangedEvents = new List<LoadingStateChangedEventArgs>();

            dataSource.PageLoaded += (sender, e) => pageLoadedEvents.Add(e);
            handler.LoadingStateChanged += (sender, e) => loadingStateChangedEvents.Add(e);

            // Act
            await handler.InitializeAsync();
            await handler.HandleScrollAsync(35, 20);

            // Assert
            Assert.NotEmpty(pageLoadedEvents);
            Assert.NotEmpty(loadingStateChangedEvents);
            Assert.Contains(pageLoadedEvents, e => e.PageNumber == 1);
            Assert.Contains(pageLoadedEvents, e => e.PageNumber == 2);
        }

        [Fact]
        public async Task SmallDataSet_ShouldHandleCorrectly()
        {
            // Arrange - Less data than one page
            var dataSource = new TestDataSource("test", 100, 25);
            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.InitializeAsync();

            // Assert
            Assert.Equal(25, handler.GetDataTable().Rows.Count);
            Assert.Equal(25, handler.TotalRows);

            // Try to scroll - should not load more
            await handler.HandleScrollAsync(20, 10);
            Assert.Equal(25, handler.GetDataTable().Rows.Count);
        }

        [Fact]
        public async Task MultipleRefreshes_ShouldResetCorrectly()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 150);
            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.InitializeAsync();
            await handler.HandleScrollAsync(35, 20);
            Assert.Equal(100, handler.GetDataTable().Rows.Count);

            await handler.RefreshAsync();
            Assert.Equal(50, handler.GetDataTable().Rows.Count);

            await handler.RefreshAsync();
            Assert.Equal(50, handler.GetDataTable().Rows.Count);

            // Assert - Should still work after multiple refreshes
            await handler.HandleScrollAsync(35, 20);
            Assert.Equal(100, handler.GetDataTable().Rows.Count);
        }

        [Fact]
        public async Task DataIntegrity_AllRowsShouldBeUnique()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 250);
            var handler = new VirtualScrollHandler(dataSource);

            // Act - Load multiple pages
            await handler.InitializeAsync();
            await handler.HandleScrollAsync(35, 20);
            await handler.HandleScrollAsync(85, 20);
            await handler.HandleScrollAsync(135, 20);

            // Assert - Check that all IDs are unique
            var dataTable = handler.GetDataTable();
            var ids = new HashSet<int>();

            foreach (DataRow row in dataTable.Rows)
            {
                int id = (int)row["Id"];
                Assert.DoesNotContain(id, ids); // ID should be unique
                ids.Add(id);
            }

            Assert.Equal(dataTable.Rows.Count, ids.Count);
        }

        [Fact]
        public async Task SequentialDataLoading_ShouldMaintainOrder()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 50, 200);
            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.InitializeAsync();
            await handler.HandleScrollAsync(35, 20);
            await handler.HandleScrollAsync(85, 20);

            // Assert - Check that IDs are in sequential order
            var dataTable = handler.GetDataTable();
            int expectedId = 1;

            foreach (DataRow row in dataTable.Rows)
            {
                int actualId = (int)row["Id"];
                Assert.Equal(expectedId, actualId);
                expectedId++;
            }
        }

        [Fact]
        public async Task ComplexScenario_MultipleOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var dataSource = new TestDataSource("test", 40, 200);
            var handler = new VirtualScrollHandler(dataSource);

            // Act - Complex sequence of operations
            await handler.InitializeAsync();
            Assert.Equal(40, handler.GetDataTable().Rows.Count);

            await handler.HandleScrollAsync(30, 15);
            var countAfterFirstScroll = handler.GetDataTable().Rows.Count;
            Assert.True(countAfterFirstScroll >= 80);

            await handler.EnsureRowLoadedAsync(100);
            Assert.True(handler.GetDataTable().Rows.Count >= 101);

            await handler.RefreshAsync();
            Assert.Equal(40, handler.GetDataTable().Rows.Count);

            await handler.HandleScrollAsync(25, 20);
            Assert.True(handler.GetDataTable().Rows.Count >= 80);

            // Assert - Final state should be consistent
            var finalData = handler.GetDataTable();
            Assert.NotNull(finalData);
            Assert.True(finalData.Rows.Count > 0);
            Assert.Equal(200, handler.TotalRows);
        }
    }
}
