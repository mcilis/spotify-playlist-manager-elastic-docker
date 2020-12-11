using System.Threading.Tasks;
using Domain;

namespace Repositories
{
    public interface ITrackRepository
    {
        Task<Track> RetrieveAsync(int trackId = -1, string code = "", string searchText = "");
        Task<int> RegisterAsync(Track track);
    }
}