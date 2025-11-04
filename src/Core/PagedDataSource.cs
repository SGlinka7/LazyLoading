using System.Data;
using System.Data.SqlClient;

namespace LazyLoading.Core
{
    /// <summary>
    /// Abstrakcyjna klasa bazowa do obsługi paginacji danych z bazy SQL.
    /// Zapewnia mechanizm lazy loading z cache'owaniem załadowanych stron.
    /// </summary>
    public abstract class PagedDataSource
    {
        private readonly Dictionary<int, DataTable> _pageCache;
        private readonly object _cacheLock = new object();

        public int PageSize { get; set; }
        public int TotalRecords { get; private set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        protected string ConnectionString { get; set; }

        public event EventHandler<PageLoadedEventArgs>? PageLoaded;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;

        protected PagedDataSource(string connectionString, int pageSize = 50)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            PageSize = pageSize;
            _pageCache = new Dictionary<int, DataTable>();
        }

        /// <summary>
        /// Pobiera stronę danych. Jeśli strona jest w cache, zwraca z cache.
        /// </summary>
        public async Task<DataTable?> GetPageAsync(int pageNumber)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1");

            lock (_cacheLock)
            {
                if (_pageCache.TryGetValue(pageNumber, out var cachedPage))
                {
                    return cachedPage;
                }
            }

            try
            {
                var page = await LoadPageFromDatabaseAsync(pageNumber);

                if (page != null)
                {
                    lock (_cacheLock)
                    {
                        if (!_pageCache.ContainsKey(pageNumber))
                        {
                            _pageCache[pageNumber] = page;
                        }
                    }

                    PageLoaded?.Invoke(this, new PageLoadedEventArgs(pageNumber, page.Rows.Count));
                }

                return page;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs($"Error loading page {pageNumber}: {ex.Message}"));
                return null;
            }
        }

        /// <summary>
        /// Pobiera wiele stron jednocześnie (przydatne do prefetch).
        /// </summary>
        public async Task<Dictionary<int, DataTable>> GetPagesAsync(params int[] pageNumbers)
        {
            var tasks = pageNumbers.Select(p => GetPageAsync(p));
            var results = await Task.WhenAll(tasks);

            var dictionary = new Dictionary<int, DataTable>();
            for (int i = 0; i < pageNumbers.Length; i++)
            {
                if (results[i] != null)
                {
                    dictionary[pageNumbers[i]] = results[i]!;
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Ładuje stronę z bazy danych. Implementowana przez klasy pochodne.
        /// </summary>
        protected virtual async Task<DataTable?> LoadPageFromDatabaseAsync(int pageNumber)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = CreateCommand(connection, pageNumber);

            await connection.OpenAsync();

            var dataTable = new DataTable();
            using var adapter = new SqlDataAdapter(command);

            await Task.Run(() => adapter.Fill(dataTable));

            // Pobierz totalCount jeśli jest parametr wyjściowy
            if (command.Parameters.Contains("@TotalCount"))
            {
                var totalCountParam = command.Parameters["@TotalCount"];
                if (totalCountParam.Value != DBNull.Value)
                {
                    TotalRecords = Convert.ToInt32(totalCountParam.Value);
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Tworzy SqlCommand dla danej strony. Musi być zaimplementowana przez klasy pochodne.
        /// </summary>
        protected abstract SqlCommand CreateCommand(SqlConnection connection, int pageNumber);

        /// <summary>
        /// Czyści cache stron.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                foreach (var page in _pageCache.Values)
                {
                    page.Dispose();
                }
                _pageCache.Clear();
            }
        }

        /// <summary>
        /// Odświeża dane - czyści cache i resetuje licznik.
        /// </summary>
        public virtual void Refresh()
        {
            ClearCache();
            TotalRecords = 0;
        }

        /// <summary>
        /// Prefetchuje następne strony w tle (opcjonalna optymalizacja).
        /// </summary>
        public async Task PrefetchNextPagesAsync(int currentPage, int pagesToPrefetch = 2)
        {
            var pagesToLoad = new List<int>();

            for (int i = 1; i <= pagesToPrefetch; i++)
            {
                int nextPage = currentPage + i;
                if (nextPage <= TotalPages)
                {
                    lock (_cacheLock)
                    {
                        if (!_pageCache.ContainsKey(nextPage))
                        {
                            pagesToLoad.Add(nextPage);
                        }
                    }
                }
            }

            if (pagesToLoad.Any())
            {
                _ = Task.Run(async () => await GetPagesAsync(pagesToLoad.ToArray()));
            }
        }
    }

    public class PageLoadedEventArgs : EventArgs
    {
        public int PageNumber { get; }
        public int RecordsLoaded { get; }

        public PageLoadedEventArgs(int pageNumber, int recordsLoaded)
        {
            PageNumber = pageNumber;
            RecordsLoaded = recordsLoaded;
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }

        public ErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
