using Domain;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Repositories
{
    internal class CredentialsRepository : ICredentialsRepository
    {
        private readonly string _connectionString;

        private SqliteConnection _connection => new SqliteConnection(_connectionString);

        public CredentialsRepository(IConfiguration configuration)
        {
            _connectionString = configuration["Repositories:ConnectionString"];
        }

        public async Task<bool> RegisterAsync(Credentials credentials)
        {            
            const string command = "UPDATE Credentials SET AccessToken=@AccessToken, CreateDate=@CreateDate";
            using var connection = _connection;
            var result = await connection.ExecuteAsync(command, credentials);

            return result == 1;
        }

        public async Task<Credentials> RetrieveAsync()
        {
            using var connection = _connection;
            return await connection.QueryFirstOrDefaultAsync<Credentials>("SELECT * FROM Credentials");
        }
    }
}