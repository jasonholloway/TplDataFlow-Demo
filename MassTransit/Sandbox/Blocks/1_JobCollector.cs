using System;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;

namespace Sandbox.Blocks 
{
    public class JobCollector : IBlock 
    {
        public string Name => "Ticks";

        public void Setup(IReceiveEndpointConfigurator x) 
        {
            x.UseRateLimit(10);
            
            x.UseExecuteAsync(async xx => {
                var batchOfJobs = new JobBatch();
                await xx.Send(new Uri("TrackingPoller"), batchOfJobs);
            });
        }
    }

    public class JobBatch 
    {
        
    }
}