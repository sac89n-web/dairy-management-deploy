using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Dairy.Infrastructure
{
    public abstract class BaseRepository
    {
        protected readonly SqlConnectionFactory _connectionFactory;
        public BaseRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<T>(sql, param);
        }

        protected async Task<int> ExecuteAsync(string sql, object? param = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteAsync(sql, param);
        }

        protected async Task<IDbTransaction> BeginTransactionAsync()
        {
            var conn = _connectionFactory.CreateConnection();
            await ((Npgsql.NpgsqlConnection)conn).OpenAsync();
            return conn.BeginTransaction();
        }
    }
}
