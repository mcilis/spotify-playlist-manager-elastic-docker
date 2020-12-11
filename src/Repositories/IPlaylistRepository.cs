using System.Threading.Tasks;
using Domain;

namespace Repositories
{
    public interface IPlaylistRepository
    {
         Task<Playlist> RetrieveAsync(int playlistId = -1, string code = "", string name = "");
         Task<int> RegisterAsync(Playlist playlist);
         Task<bool> AddTrackAsync(Playlist playlist, Track track);
         Task<bool> ContainsTrackAsync(Playlist playlist, Track track);
    }
}