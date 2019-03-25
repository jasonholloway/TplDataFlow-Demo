using System;
using System.Threading;
using Castle.Windsor;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using Sandbox.Blocks;

namespace Sandbox 
{
    public class Program 
    {
        public static void Main() 
        {
            var container = CreateContainer();
            
            var bus = InMemoryBusForTesting();
            
            var cancelSource = new CancellationTokenSource();
            bus.StartAsync(cancelSource.Token);
            
            Console.ReadLine();
            cancelSource.Cancel();
            bus.StopAsync().Wait();
        }
        
        
        static IBusControl InMemoryBusForTesting() 
            => Bus.Factory.CreateUsingInMemory(
                x => {
                    x.RegisterBlock(new JobCollector());
                    x.RegisterBlock(new TrackingPoller());
                    x.RegisterBlock(new TrackingMapper());
                    x.RegisterBlock(new TrackingSink());
                });
      

        static IBusControl AzureBus() 
            => Bus.Factory.CreateUsingAzureServiceBus(
                x => {
                    var host = x.Host("connectionString", sb => { });
                    var subscription = "subscriptionName";
                    
                    x.SubscriptionEndpoint(host, subscription, "topicPath", null);
                    x.SubscriptionEndpoint(host, subscription, "topicPath", null);
                    x.SubscriptionEndpoint(host, subscription, "topicPath", null);
                });


        static IWindsorContainer CreateContainer()
            => new WindsorContainer()
                .Register(/****/);
        
    }
}