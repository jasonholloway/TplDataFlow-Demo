using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;

namespace Sandbox.Blocks 
{
    public class TrackingSink : IBlock 
    {
        public string Name => "TrackingSink";
        
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