using System.Threading.Tasks;
using Domain;

namespace Repositories
{
    public interface ICredentialsRepository
    {
         Task<Credentials> RetrieveAsync();
         Task<bool> RegisterAsync(Credentials credentials);
    }
}