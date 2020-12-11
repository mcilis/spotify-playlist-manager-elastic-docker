using Domain;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Repositories
{
    internal class TrackRepository : ITrackRepository
    {
        private readonly string _connectionString;

        private SqliteConnection _connection => new SqliteConnection(_connectionString);

        public TrackRepository(IConfiguration configuration)
        {
            _connectionString = configuration["Repositories:ConnectionString"];
        }

        public async Task<int> RegisterAsync(Track track)
        {            
            const string command = @"INSERT INTO Track (Code, Name, Popularity, Uri, Artist, SearchText, CreateDate)
                                     VALUES(@Code, @Name, @Popularity, @Uri, @Artist, @SearchText, @CreateDate); 
                                     SELECT last_insert_rowid();";

            using var connection = _connection;
            var trackId = await connection.ExecuteScalarAsync<int>(command, track);

            return trackId;
        }

         public async Task<Track> RetrieveAsync(int trackId = -1, string code = "", string searchText = "")
        {
            var parameters = new DynamicParameters();
            var query = "SELECT * FROM Track ";

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query += "WHERE SearchText = @SearchText";
                parameters.AddDynamicParams(new { SearchText = searchText });
            }
            else if (!string.IsNullOrWhiteSpace(code))
            {
                query += "WHERE Code = @Code";
                parameters.AddDynamicParams(new { Code = code });
            }
            else if (trackId != -1)
            {
                query += "WHERE TrackId = @TrackId";
                parameters.AddDynamicParams(new { TrackId = trackId });
            }

            using var connection = _connection;
            return await connection.QueryFirstOrDefaultAsync<Track>(query, parameters);
        }
    }
}