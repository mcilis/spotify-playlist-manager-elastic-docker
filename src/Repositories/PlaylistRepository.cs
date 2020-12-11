using Domain;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace Repositories
{
    internal class PlaylistRepository : IPlaylistRepository
    {
        private readonly string _connectionString;

        private SqliteConnection _connection => new SqliteConnection(_connectionString);

        public PlaylistRepository(IConfiguration configuration)
        {
            _connectionString = configuration["Repositories:ConnectionString"];
        }

        public async Task<int> RegisterAsync(Playlist playlist)
        {
            var parameters = new DynamicParameters();
            parameters.RemoveUnused = true;

            using var connection = _connection;

            parameters.AddDynamicParams(new { Code = playlist.Code });
            var existingPlaylist = await connection.QueryFirstOrDefaultAsync<Playlist>("SELECT * FROM Playlist WHERE Code=@Code", parameters);

            if (existingPlaylist != null)
            {
                const string updateCommand = "UPDATE Playlist SET TracksCount=@TracksCount WHERE PlaylistId=@PlaylistId";
                
                parameters.AddDynamicParams(new { TracksCount = playlist.TracksCount });
                parameters.AddDynamicParams(new { PlaylistId = existingPlaylist.PlaylistId });

                var updatedRowCount = await connection.ExecuteAsync(updateCommand, parameters);
                if (updatedRowCount != 1)
                {
                    throw new Exception($"Failed to update playlist! updatedRowCount:{updatedRowCount}, id:{existingPlaylist.PlaylistId}");
                }
                return existingPlaylist.PlaylistId;
            }

            const string command = @"INSERT INTO Playlist (Code, Name, Uri, TracksCount, Public, CreateDate)
                                     VALUES(@Code, @Name, @Uri, @TracksCount, @Public, @CreateDate);
                                     SELECT last_insert_rowid();";

            var playlistId = await connection.ExecuteScalarAsync<int>(command, playlist);
            return playlistId;
        }

        public async Task<Playlist> RetrieveAsync(int playlistId = -1, string code = "", string name = "")
        {
            var parameters = new DynamicParameters();
            var query = "SELECT * FROM Playlist ";

            if (!string.IsNullOrWhiteSpace(name))
            {
                query += "WHERE Name = @Name";
                parameters.AddDynamicParams(new { Name = name });
            }
            else if (!string.IsNullOrWhiteSpace(code))
            {
                query += "WHERE Code = @Code";
                parameters.AddDynamicParams(new { Code = code });
            }
            else if (playlistId != -1)
            {
                query += "WHERE PlaylistId = @PlaylistId";
                parameters.AddDynamicParams(new { PlaylistId = playlistId });
            }

            using var connection = _connection;
            return await connection.QueryFirstOrDefaultAsync<Playlist>(query, parameters);
        }

        public async Task<bool> AddTrackAsync(Playlist playlist, Track track)
        {
            if (playlist.PlaylistId == 0 || track.TrackId == 0)
            {
                throw new System.ArgumentException("Undefined playlist or track!");
            }

            const string command = @"INSERT INTO PlaylistTrack (PlaylistId, TrackId, CreateDate)
                                        VALUES(@PlaylistId, @TrackId, @CreateDate)";

            var parameters = new DynamicParameters();
            parameters.AddDynamicParams(new { PlaylistId = playlist.PlaylistId });
            parameters.AddDynamicParams(new { TrackId = track.TrackId });
            parameters.AddDynamicParams(new { CreateDate = DateTime.UtcNow });

            using var connection = _connection;
            var insertedRowCount = await connection.ExecuteAsync(command, parameters);
            return insertedRowCount == 1;
        }

        public async Task<bool> ContainsTrackAsync(Playlist playlist, Track track)
        {
            if (playlist.PlaylistId == 0 || track.TrackId == 0)
            {
                throw new System.ArgumentException("Undefined playlist or track!");
            }

            const string query = "SELECT 1 FROM PlaylistTrack WHERE PlaylistId=@PlaylistId AND TrackId=@TrackId";

            var parameters = new DynamicParameters();
            parameters.AddDynamicParams(new { PlaylistId = playlist.PlaylistId });
            parameters.AddDynamicParams(new { TrackId = track.TrackId });

            using var connection = _connection;
            var result = await connection.QueryFirstOrDefaultAsync<int>(query, parameters);

            return result == 1;
        }
    }
}