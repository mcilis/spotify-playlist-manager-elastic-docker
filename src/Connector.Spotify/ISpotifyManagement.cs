using System.Collections.Generic;
using System.Threading.Tasks;
using Domain;

namespace Connector.Spotify
{
    public interface ISpotifyManagement
    {
        public Task<Track> SearchForATrackAsync(string query);
        public Task<List<Playlist>> GetPlaylistsAsync(string ownerId = "mcilis", int offset = 0, int limit = 50);
        public Task<Playlist> FindPlaylistAsync(string playlistName, string ownerId = "mcilis");
        public Task<Playlist> CreatePlaylistAsync(string playlistName, bool isPublic = true, string ownerId = "mcilis");
        public Task<bool> AddTrackToPlaylistAsync(Playlist playlist, Track track, string ownerId = "mcilis");
    }
}