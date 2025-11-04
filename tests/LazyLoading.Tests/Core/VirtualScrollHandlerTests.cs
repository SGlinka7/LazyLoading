using System.Data;
using LazyLoading.Core;
using Moq;
using Xunit;

namespace LazyLoading.Tests.Core
{
    /// <summary>
    /// Testy jednostkowe dla klasy VirtualScrollHandler.
    /// </summary>
    public class VirtualScrollHandlerTests
    {
        // Mock PagedDataSource dla testów
        private class MockPagedDataSource : PagedDataSource
        {
            private readonly Dictionary<int, DataTable> _pages;

            public MockPagedDataSource(string connectionString, int pageSize, int totalRecords)
                : base(connectionString, pageSize)
            {
                _pages = new Dictionary<int, DataTable>();

                // Ustaw TotalRecords
                typeof(PagedDataSource)
                    .GetProperty("TotalRecords")!
                    .SetValue(this, totalRecords);
            }

            public void AddPage(int pageNumber, DataTable data)
            {
                _pages[pageNumber] = data;
            }

            protected override System.Data.SqlClient.SqlCommand CreateCommand(
                System.Data.SqlClient.SqlConnection connection,
                int pageNumber)
            {
                throw new NotImplementedException();
            }

            protected override async Task<DataTable?> LoadPageFromDatabaseAsync(int pageNumber)
            {
                await Task.Delay(10); // Symuluj opóźnienie

                if (_pages.TryGetValue(pageNumber, out var page))
                {
                    return page;
                }

                return CreateEmptyDataTable();
            }

            private DataTable CreateEmptyDataTable()
            {
                var table = new DataTable();
                table.Columns.Add("Id", typeof(int));
                table.Columns.Add("Name", typeof(string));
                return table;
            }
        }

        [Fact]
        public void Constructor_WithValidDataSource_ShouldInitialize()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);

            // Act
            var handler = new VirtualScrollHandler(dataSource);

            // Assert
            Assert.NotNull(handler);
            Assert.Equal(50, handler.RowsPerPage);
            Assert.Equal(100, handler.TotalRows);
        }

        [Fact]
        public void Constructor_WithNullDataSource_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new VirtualScrollHandler(null!));
        }

        [Fact]
        public async Task InitializeAsync_ShouldLoadFirstPage()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);
            var firstPage = CreateTestDataTable(50, 1);
            dataSource.AddPage(1, firstPage);

            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.InitializeAsync();

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.NotNull(dataTable);
            Assert.Equal(50, dataTable.Rows.Count);
        }

        [Fact]
        public async Task InitializeAsync_ShouldRaiseLoadingStateChangedEvents()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);
            var firstPage = CreateTestDataTable(50, 1);
            dataSource.AddPage(1, firstPage);

            var handler = new VirtualScrollHandler(dataSource);

            var loadingStates = new List<bool>();
            handler.LoadingStateChanged += (sender, e) => loadingStates.Add(e.IsLoading);

            // Act
            await handler.InitializeAsync();

            // Assert
            Assert.Equal(2, loadingStates.Count);
            Assert.True(loadingStates[0]); // Started loading
            Assert.False(loadingStates[1]); // Finished loading
        }

        [Fact]
        public async Task GetDataTable_ShouldReturnVirtualDataTable()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);
            var firstPage = CreateTestDataTable(50, 1);
            dataSource.AddPage(1, firstPage);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Act
            var dataTable = handler.GetDataTable();

            // Assert
            Assert.NotNull(dataTable);
            Assert.Equal(50, dataTable.Rows.Count);
        }

        [Fact]
        public async Task HandleScrollAsync_WhenNearEnd_ShouldLoadNextPage()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 150);
            var page1 = CreateTestDataTable(50, 1);
            var page2 = CreateTestDataTable(50, 2);
            dataSource.AddPage(1, page1);
            dataSource.AddPage(2, page2);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Act
            await handler.HandleScrollAsync(35, 20); // Scroll near the end (row 55 visible)

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.Equal(100, dataTable.Rows.Count); // First page + second page
        }

        [Fact]
        public async Task HandleScrollAsync_WhenNotNearEnd_ShouldNotLoadMore()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 150);
            var page1 = CreateTestDataTable(50, 1);
            dataSource.AddPage(1, page1);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Act
            await handler.HandleScrollAsync(0, 20); // Scroll at the beginning

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.Equal(50, dataTable.Rows.Count); // Only first page
        }

        [Fact]
        public async Task HandleScrollAsync_WhenAlreadyLoading_ShouldNotLoadAgain()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 150);
            var page1 = CreateTestDataTable(50, 1);
            var page2 = CreateTestDataTable(50, 2);
            dataSource.AddPage(1, page1);
            dataSource.AddPage(2, page2);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Act - uruchom równoległe scrolle
            var task1 = handler.HandleScrollAsync(35, 20);
            var task2 = handler.HandleScrollAsync(35, 20);
            await Task.WhenAll(task1, task2);

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.Equal(100, dataTable.Rows.Count); // Powinno załadować tylko raz
        }

        [Fact]
        public async Task HandleScrollAsync_AtLastPage_ShouldNotLoadMore()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);
            var page1 = CreateTestDataTable(50, 1);
            var page2 = CreateTestDataTable(50, 2);
            dataSource.AddPage(1, page1);
            dataSource.AddPage(2, page2);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Load second page
            await handler.HandleScrollAsync(35, 20);

            var rowCountBefore = handler.GetDataTable().Rows.Count;

            // Act - try to scroll beyond last page
            await handler.HandleScrollAsync(85, 20);

            // Assert
            var rowCountAfter = handler.GetDataTable().Rows.Count;
            Assert.Equal(rowCountBefore, rowCountAfter); // Nie załadowano więcej
        }

        [Fact]
        public async Task RefreshAsync_ShouldClearAndReloadData()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);
            var page1 = CreateTestDataTable(50, 1);
            var page2 = CreateTestDataTable(50, 2);
            dataSource.AddPage(1, page1);
            dataSource.AddPage(2, page2);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Load second page
            await handler.HandleScrollAsync(35, 20);

            // Act
            await handler.RefreshAsync();

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.Equal(50, dataTable.Rows.Count); // Tylko pierwsza strona po refresh
        }

        [Fact]
        public async Task EnsureRowLoadedAsync_WhenRowAlreadyLoaded_ShouldNotLoadMore()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);
            var page1 = CreateTestDataTable(50, 1);
            dataSource.AddPage(1, page1);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Act
            await handler.EnsureRowLoadedAsync(25); // Row already loaded

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.Equal(50, dataTable.Rows.Count);
        }

        [Fact]
        public async Task EnsureRowLoadedAsync_WhenRowNotLoaded_ShouldLoadRequiredPages()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 150);
            var page1 = CreateTestDataTable(50, 1);
            var page2 = CreateTestDataTable(50, 2);
            var page3 = CreateTestDataTable(50, 3);
            dataSource.AddPage(1, page1);
            dataSource.AddPage(2, page2);
            dataSource.AddPage(3, page3);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Act
            await handler.EnsureRowLoadedAsync(125); // Row on page 3

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.True(dataTable.Rows.Count >= 126); // All 3 pages loaded
        }

        [Fact]
        public async Task LoadingStateChanged_ShouldReflectCorrectState()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 100);
            var page1 = CreateTestDataTable(50, 1);
            var page2 = CreateTestDataTable(50, 2);
            dataSource.AddPage(1, page1);
            dataSource.AddPage(2, page2);

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            var loadingStates = new List<bool>();
            handler.LoadingStateChanged += (sender, e) => loadingStates.Add(e.IsLoading);

            // Act
            await handler.HandleScrollAsync(35, 20);

            // Assert
            Assert.Contains(true, loadingStates); // Was loading
            Assert.Contains(false, loadingStates); // Finished loading
            Assert.False(loadingStates.Last()); // Final state should be not loading
        }

        [Fact]
        public async Task MultiplePageLoads_ShouldAccumulateData()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 30, 150);
            for (int i = 1; i <= 5; i++)
            {
                dataSource.AddPage(i, CreateTestDataTable(30, i));
            }

            var handler = new VirtualScrollHandler(dataSource);
            await handler.InitializeAsync();

            // Act - Load pages progressively
            await handler.HandleScrollAsync(15, 20); // Load page 2
            await handler.HandleScrollAsync(45, 20); // Load page 3
            await handler.HandleScrollAsync(75, 20); // Load page 4

            // Assert
            var dataTable = handler.GetDataTable();
            Assert.Equal(120, dataTable.Rows.Count); // 4 pages * 30 rows
        }

        [Fact]
        public void RowsPerPage_ShouldReflectDataSourcePageSize()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 75, 200);

            // Act
            var handler = new VirtualScrollHandler(dataSource);

            // Assert
            Assert.Equal(75, handler.RowsPerPage);
        }

        [Fact]
        public void TotalRows_ShouldReflectDataSourceTotalRecords()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 200);

            // Act
            var handler = new VirtualScrollHandler(dataSource);

            // Assert
            Assert.Equal(200, handler.TotalRows);
        }

        [Fact]
        public async Task HandleScrollAsync_WithZeroTotalRecords_ShouldNotLoad()
        {
            // Arrange
            var dataSource = new MockPagedDataSource("test", 50, 0);
            var handler = new VirtualScrollHandler(dataSource);

            // Act
            await handler.HandleScrollAsync(0, 20);

            // Assert - nie powinno rzucić wyjątku
            Assert.NotNull(handler.GetDataTable());
        }

        // Helper method
        private DataTable CreateTestDataTable(int rowCount, int pageId)
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
