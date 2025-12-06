using MySql.Data.MySqlClient;
using System.Data;

namespace MyFirstApi.Services
{
    /// <summary>
    /// Base database service providing common database operations
    /// Reduces code duplication across controllers
    /// </summary>
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get a new database connection
        /// </summary>
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        /// <summary>
        /// Execute a stored procedure with parameters
        /// </summary>
        public async Task<T?> ExecuteStoredProcedureAsync<T>(
            string procedureName,
            Dictionary<string, object> parameters,
            Func<MySqlDataReader, T> mapFunction)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                using var cmd = new MySqlCommand($"{procedureName}", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
                if (reader != null && await reader.ReadAsync())
                {
                    return mapFunction(reader);
                }

                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing stored procedure {procedureName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute a stored procedure that returns multiple rows
        /// </summary>
        public async Task<List<T>> ExecuteStoredProcedureListAsync<T>(
            string procedureName,
            Dictionary<string, object> parameters,
            Func<MySqlDataReader, T> mapFunction)
        {
            var results = new List<T>();

            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                using var cmd = new MySqlCommand($"{procedureName}", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
                if (reader != null)
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(mapFunction(reader));
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing stored procedure {procedureName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute a query that returns a single value (scalar)
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                var result = await cmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    return default;

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing scalar query: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute a non-query command (INSERT, UPDATE, DELETE)
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing non-query: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute a query and return a single row
        /// </summary>
        public async Task<T?> ExecuteQuerySingleAsync<T>(
            string query,
            Dictionary<string, object>? parameters,
            Func<MySqlDataReader, T> mapFunction)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
                if (reader != null && await reader.ReadAsync())
                {
                    return mapFunction(reader);
                }

                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing query: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute a query and return multiple rows
        /// </summary>
        public async Task<List<T>> ExecuteQueryListAsync<T>(
            string query,
            Dictionary<string, object>? parameters,
            Func<MySqlDataReader, T> mapFunction)
        {
            var results = new List<T>();

            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
                if (reader != null)
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(mapFunction(reader));
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing query: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute a transaction with multiple commands
        /// </summary>
        public async Task<bool> ExecuteTransactionAsync(Func<MySqlConnection, MySqlTransaction, Task> transactionAction)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                await transactionAction(conn, transaction);
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Transaction failed: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Check if a record exists
        /// </summary>
        public async Task<bool> ExistsAsync(string query, Dictionary<string, object>? parameters = null)
        {
            var count = await ExecuteScalarAsync<int>(query, parameters);
            return count > 0;
        }

        /// <summary>
        /// Get safe string value from reader (handles null)
        /// </summary>
        public static string GetSafeString(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        /// <summary>
        /// Get safe int value from reader (handles null)
        /// </summary>
        public static int? GetSafeInt(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        /// <summary>
        /// Get safe DateTime value from reader (handles null)
        /// </summary>
        public static DateTime? GetSafeDateTime(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        /// <summary>
        /// Get safe decimal value from reader (handles null)
        /// </summary>
        public static decimal? GetSafeDecimal(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }

        /// <summary>
        /// Get safe bool value from reader (handles null)
        /// </summary>
        public static bool? GetSafeBool(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
        }
    }
}
