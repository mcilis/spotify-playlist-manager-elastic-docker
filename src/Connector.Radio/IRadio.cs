using System.Threading.Tasks;

namespace Connector.Radio
{
    public interface IRadio
    {
        string Name { get; }

        Task<string> GetCurrentSongAsync();
    }
}
