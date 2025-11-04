using System.Data;

namespace LazyLoading.Core
{
    /// <summary>
    /// Obsługuje wirtualne scrollowanie dla DataGridView z lazy loading.
    /// </summary>
    public class VirtualScrollHandler
    {
        private readonly PagedDataSource _dataSource;
        private readonly DataTable _virtualDataTable;
        private int _currentDisplayedPage = 0;
        private bool _isLoading = false;

        public int RowsPerPage => _dataSource.PageSize;
        public int TotalRows => _dataSource.TotalRecords;

        public event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;

        public VirtualScrollHandler(PagedDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _virtualDataTable = new DataTable();
        }

        /// <summary>
        /// Inicjalizuje handler - ładuje pierwszą stronę i ustawia strukturę tabeli.
        /// </summary>
        public async Task InitializeAsync()
        {
            SetLoadingState(true);

            try
            {
                var firstPage = await _dataSource.GetPageAsync(1);

                if (firstPage != null && firstPage.Rows.Count > 0)
                {
                    // Kopiuj strukturę z pierwszej strony
                    _virtualDataTable.Clear();
                    _virtualDataTable.Columns.Clear();

                    foreach (DataColumn column in firstPage.Columns)
                    {
                        _virtualDataTable.Columns.Add(column.ColumnName, column.DataType);
                    }

                    // Dodaj wiersze z pierwszej strony
                    foreach (DataRow row in firstPage.Rows)
                    {
                        _virtualDataTable.ImportRow(row);
                    }

                    _currentDisplayedPage = 1;

                    // Prefetch następnej strony
                    _ = _dataSource.PrefetchNextPagesAsync(1, 2);
                }
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Pobiera DataTable do bindowania z gridem.
        /// </summary>
        public DataTable GetDataTable()
        {
            return _virtualDataTable;
        }

        /// <summary>
        /// Sprawdza czy potrzeba załadować więcej danych na podstawie pozycji scrolla.
        /// </summary>
        public async Task HandleScrollAsync(int firstDisplayedRowIndex, int displayedRowCount)
        {
            if (_isLoading || _dataSource.TotalRecords == 0)
                return;

            int lastVisibleRow = firstDisplayedRowIndex + displayedRowCount;
            int currentRowCount = _virtualDataTable.Rows.Count;

            // Sprawdź czy użytkownik zbliża się do końca załadowanych danych
            if (lastVisibleRow >= currentRowCount - 10) // 10 wierszy przed końcem
            {
                int nextPage = _currentDisplayedPage + 1;

                if (nextPage <= _dataSource.TotalPages)
                {
                    await LoadNextPageAsync();
                }
            }
        }

        /// <summary>
        /// Ładuje następną stronę danych.
        /// </summary>
        private async Task LoadNextPageAsync()
        {
            if (_isLoading)
                return;

            SetLoadingState(true);

            try
            {
                int nextPage = _currentDisplayedPage + 1;
                var pageData = await _dataSource.GetPageAsync(nextPage);

                if (pageData != null && pageData.Rows.Count > 0)
                {
                    foreach (DataRow row in pageData.Rows)
                    {
                        _virtualDataTable.ImportRow(row);
                    }

                    _currentDisplayedPage = nextPage;

                    // Prefetch kolejnych stron
                    _ = _dataSource.PrefetchNextPagesAsync(nextPage, 2);
                }
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Odświeża dane - czyści wszystko i ładuje od nowa.
        /// </summary>
        public async Task RefreshAsync()
        {
            _dataSource.Refresh();
            _virtualDataTable.Clear();
            _currentDisplayedPage = 0;

            await InitializeAsync();
        }

        /// <summary>
        /// Wymusza załadowanie danych do określonego wiersza.
        /// </summary>
        public async Task EnsureRowLoadedAsync(int rowIndex)
        {
            if (rowIndex < _virtualDataTable.Rows.Count)
                return;

            int requiredPage = (rowIndex / RowsPerPage) + 1;

            while (_currentDisplayedPage < requiredPage && _currentDisplayedPage < _dataSource.TotalPages)
            {
                await LoadNextPageAsync();
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;
            LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs(isLoading));
        }
    }

    public class LoadingStateChangedEventArgs : EventArgs
    {
        public bool IsLoading { get; }

        public LoadingStateChangedEventArgs(bool isLoading)
        {
            IsLoading = isLoading;
        }
    }
}
