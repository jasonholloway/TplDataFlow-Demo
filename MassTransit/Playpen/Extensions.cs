using MassTransit;

namespace Sandbox 
{
    public static class BusFactoryExtensions 
    {
        public static void RegisterBlock(this IBusFactoryConfigurator x, IBlock block)
            => x.ReceiveEndpoint(block.Name, block.Setup);
    }
}