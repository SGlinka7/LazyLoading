using System.Data;
using System.Data.SqlClient;

namespace LazyLoading.Core
{
    /// <summary>
    /// Generyczna implementacja PagedDataSource dla szybkiego użycia.
    /// Idealny do prostych przypadków bez potrzeby tworzenia dedykowanej klasy.
    /// </summary>
    public class GenericPagedDataSource : PagedDataSource
    {
        private readonly string _storedProcedureName;
        private readonly Dictionary<string, object> _parameters;

        /// <summary>
        /// Tworzy generyczny data source dla dowolnej procedury składowanej.
        /// </summary>
        /// <param name="connectionString">Connection string do bazy</param>
        /// <param name="storedProcedureName">Nazwa procedury składowanej</param>
        /// <param name="pageSize">Rozmiar strony</param>
        /// <param name="parameters">Dodatkowe parametry dla procedury (opcjonalne)</param>
        public GenericPagedDataSource(
            string connectionString,
            string storedProcedureName,
            int pageSize = 50,
            Dictionary<string, object>? parameters = null)
            : base(connectionString, pageSize)
        {
            _storedProcedureName = storedProcedureName ?? throw new ArgumentNullException(nameof(storedProcedureName));
            _parameters = parameters ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Dodaje lub aktualizuje parametr procedury.
        /// </summary>
        public void SetParameter(string name, object value)
        {
            _parameters[name] = value;
        }

        /// <summary>
        /// Usuwa parametr.
        /// </summary>
        public void RemoveParameter(string name)
        {
            _parameters.Remove(name);
        }

        protected override SqlCommand CreateCommand(SqlConnection connection, int pageNumber)
        {
            var command = new SqlCommand(_storedProcedureName, connection);
            command.CommandType = CommandType.StoredProcedure;

            // Standardowe parametry paginacji
            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", PageSize);

            // Dodatkowe parametry użytkownika
            foreach (var param in _parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            // Parametr wyjściowy
            var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int);
            totalCountParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(totalCountParam);

            return command;
        }

        public override void Refresh()
        {
            base.Refresh();
            // Parametry pozostają niezmienione przy refresh
        }
    }
}
