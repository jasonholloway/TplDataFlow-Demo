using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;

namespace Sandbox.Blocks 
{
    public class TrackingMapper : IBlock 
    {
        public string Name => "TrackingMapper";
        
        public void Setup(IReceiveEndpointConfigurator x)
        {
            x.UseRateLimit(10);
            
            x.UseExecuteAsync(async xx => {
                await Task.Delay(20);
                Console.WriteLine("Hello!");
            });
        }
    }
}