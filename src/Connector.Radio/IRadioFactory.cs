using System.Collections.Generic;

namespace Connector.Radio
{
    public interface IRadioFactory
    {
        void Register(IRadio radio);
        IRadio Resolve(string name);

        List<IRadio> ResolveAll();
    }
}
