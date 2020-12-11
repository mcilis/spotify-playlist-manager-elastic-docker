using System.Collections.Generic;
using System.Linq;

namespace Connector.Radio
{
    internal class RadioFactory : IRadioFactory
    {
        private readonly Dictionary<string, IRadio> _radioImplementations = new Dictionary<string, IRadio>();

        public void Register(IRadio radio)
        {
            _radioImplementations.Add(radio.Name, radio);
        }

        public IRadio Resolve(string name)
        {
            return _radioImplementations[name];
        }

        public List<IRadio> ResolveAll() => _radioImplementations.Values.ToList();
    }
}
