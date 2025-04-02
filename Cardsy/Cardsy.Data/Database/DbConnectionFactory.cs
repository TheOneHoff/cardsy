using Npgsql;
using System.Data;

namespace Cardsy.Data.Database
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    }
    public class NpgsqlDbConnectionFactory : IDbConnectionFactory
    {
        readonly string _connectionString;

        public NpgsqlDbConnectionFactory(string? connection)
        {
            if (string.IsNullOrWhiteSpace(connection)) throw new ArgumentNullException(nameof(connection));
            _connectionString = connection;
        }

        public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
