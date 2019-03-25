using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;

namespace Sandbox.Blocks 
{
    public class TrackingPoller : IBlock 
    {
        public string Name => "TrackingPoller";
        
        public void Setup(IReceiveEndpointConfigurator x)
        {
            x.UseRateLimit(10);
            x.UseRetry(r => r.Immediate(3));
            x.ConsumerWithDI<Consumer>();
        }

        public class Consumer: IConsumer<JobBatch> 
        {
            readonly IPoller _poller;

            public Consumer(IPoller poller) 
            {
                _poller = poller;
            }
            
            public async Task Consume(ConsumeContext<JobBatch> x)
            {
                var raw = await _poller.Poll();
                await x.Send(new Uri("TrackingMapper"), raw);
            }
        }
    }
    
    
    
    public class RawTracking {}


    public interface IPoller 
    {
        Task<RawTracking> Poll();
    }


    public static class Extensions 
    {
        public static void ConsumerWithDI<TConsumer>(this IReceiveEndpointConfigurator x) where TConsumer : class, IConsumer 
        {
            //...
        }
    }
    
}