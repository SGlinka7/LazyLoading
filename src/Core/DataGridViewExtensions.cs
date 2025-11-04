using LazyLoading.Controls;

namespace LazyLoading.Core
{
    /// <summary>
    /// Extension methods dla łatwiejszej integracji z istniejącymi projektami.
    /// </summary>
    public static class DataGridViewExtensions
    {
        /// <summary>
        /// Konwertuje zwykły DataGridView na PagedDataGridView (jeśli to możliwe).
        /// </summary>
        public static bool TryEnablePaging(this DataGridView grid, PagedDataSource dataSource)
        {
            if (grid is PagedDataGridView pagedGrid)
            {
                pagedGrid.PagedDataSource = dataSource;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Szybka konfiguracja dla optymalnej wydajności.
        /// </summary>
        public static void ConfigureForPaging(this DataGridView grid)
        {
            grid.DoubleBuffered(true);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.RowHeadersVisible = false;
        }

        /// <summary>
        /// Helper do ustawienia DoubleBuffered przez reflection (dla zwykłego DataGridView).
        /// </summary>
        private static void DoubleBuffered(this DataGridView grid, bool enabled)
        {
            var propertyInfo = grid.GetType().GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            propertyInfo?.SetValue(grid, enabled, null);
        }
    }

    /// <summary>
    /// Helper do tworzenia connection stringów.
    /// </summary>
    public static class ConnectionStringBuilder
    {
        /// <summary>
        /// Tworzy connection string dla Integrated Security.
        /// </summary>
        public static string BuildIntegratedSecurity(string server, string database)
        {
            return $"Server={server};Database={database};Integrated Security=true;MultipleActiveResultSets=true;";
        }

        /// <summary>
        /// Tworzy connection string z username i password.
        /// </summary>
        public static string BuildSqlAuth(string server, string database, string username, string password)
        {
            return $"Server={server};Database={database};User Id={username};Password={password};MultipleActiveResultSets=true;";
        }

        /// <summary>
        /// Dodaje timeout do istniejącego connection stringa.
        /// </summary>
        public static string WithTimeout(this string connectionString, int timeoutSeconds)
        {
            return connectionString.TrimEnd(';') + $";Connection Timeout={timeoutSeconds};";
        }

        /// <summary>
        /// Dodaje max pool size.
        /// </summary>
        public static string WithPoolSize(this string connectionString, int maxPoolSize)
        {
            return connectionString.TrimEnd(';') + $";Max Pool Size={maxPoolSize};";
        }
    }
}
