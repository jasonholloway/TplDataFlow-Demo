using System.Threading.Tasks;
using MassTransit;

namespace Sandbox 
{
    public interface IBlock
    {
        string Name { get; }
        void Setup(IReceiveEndpointConfigurator x);
    }
}