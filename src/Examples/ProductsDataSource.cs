using System.Data.SqlClient;
using LazyLoading.Core;

namespace LazyLoading.Examples
{
    /// <summary>
    /// Przykładowa implementacja PagedDataSource dla tabeli Products.
    /// Pokazuje jak łatwo zintegrować paginację z konkretną procedurą SQL.
    /// </summary>
    public class ProductsDataSource : PagedDataSource
    {
        public ProductsDataSource(string connectionString, int pageSize = 50)
            : base(connectionString, pageSize)
        {
        }

        protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
        {
            var command = new SqlCommand("Products_SelectView_Paged", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            // Parametry wejściowe
            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", PageSize);

            // Parametr wyjściowy dla total count
            var totalCountParam = new SqlParameter("@TotalCount", System.Data.SqlDbType.Int);
            totalCountParam.Direction = System.Data.ParameterDirection.Output;
            command.Parameters.Add(totalCountParam);

            return command;
        }
    }

    /// <summary>
    /// Przykład z filtrowaniem - procedura z dodatkowymi parametrami.
    /// </summary>
    public class ProductsByCategoryDataSource : PagedDataSource
    {
        public string? Category { get; set; }
        public string? SearchText { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public ProductsByCategoryDataSource(string connectionString, int pageSize = 50)
            : base(connectionString, pageSize)
        {
        }

        protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
        {
            var command = new SqlCommand("Products_SelectByCategory_Paged", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            // Parametry wejściowe
            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", PageSize);
            command.Parameters.AddWithValue("@Category", (object?)Category ?? DBNull.Value);
            command.Parameters.AddWithValue("@SearchText", (object?)SearchText ?? DBNull.Value);
            command.Parameters.AddWithValue("@MinPrice", (object?)MinPrice ?? DBNull.Value);
            command.Parameters.AddWithValue("@MaxPrice", (object?)MaxPrice ?? DBNull.Value);

            // Parametr wyjściowy
            var totalCountParam = new SqlParameter("@TotalCount", System.Data.SqlDbType.Int);
            totalCountParam.Direction = System.Data.ParameterDirection.Output;
            command.Parameters.Add(totalCountParam);

            return command;
        }

        public override void Refresh()
        {
            base.Refresh();
            // Możesz dodać dodatkową logikę odświeżania
        }
    }

    /// <summary>
    /// Przykład z dynamicznym sortowaniem.
    /// </summary>
    public class ProductsWithSortDataSource : PagedDataSource
    {
        public string SortColumn { get; set; } = "ProductId";
        public string SortDirection { get; set; } = "ASC";

        public ProductsWithSortDataSource(string connectionString, int pageSize = 50)
            : base(connectionString, pageSize)
        {
        }

        protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
        {
            var command = new SqlCommand("Products_SelectView_PagedWithSort", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", PageSize);
            command.Parameters.AddWithValue("@SortColumn", SortColumn);
            command.Parameters.AddWithValue("@SortDirection", SortDirection);

            var totalCountParam = new SqlParameter("@TotalCount", System.Data.SqlDbType.Int);
            totalCountParam.Direction = System.Data.ParameterDirection.Output;
            command.Parameters.Add(totalCountParam);

            return command;
        }
    }
}
