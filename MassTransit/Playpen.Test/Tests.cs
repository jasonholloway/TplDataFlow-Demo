using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.AzureServiceBusTransport;
using Microsoft.ServiceBus;
using NUnit.Framework;

namespace Playpen.Test
{
    public class MassTransitConfiguration
        {
            public MassTransitConfiguration()
            {
                AzureNamespace = "sorted-feature8";
                AzureKey = "pTTdsm6F9yN7j0WzWUspRYakgwm8Qi24jrhffp1fkJI=";
                AzureKeyName = "sortedfeature8key";
                AzureQueueName = "MPD.Services.Tracking_Consignments_RegisterConsignment";//"MPD.Services.Tracking_Consignments_PubSub";
            }

            public string AzureNamespace { get; }

            public string AzureQueueName { get; }

            public string AzureKeyName { get; }

            public string AzureKey { get; }
        }

    [TestFixture]
    public class PublishToBus
    {
        [Test]
        public async Task Blah()
        {
            var customerReference = Guid.Parse("e8188f52-65cf-4ea1-861f-1c6df093079d");

            var message = new ConsignmentPackageTrackingEvent
            {
                CustomerReference = customerReference,
                CarrierEventDescription = "CarrierEventDescription",
                CarrierEventLocation = "CarrierEventLocation",
                PackageStatus = "PackageStatus",
                CarrierEventName = "CarrierEventName",
                CarrierEventCode = "CarrierEventCode",
                PackageTrackingReference = "PackageTrackingRef",
                ConsignmentTrackingReference = "trackingRef",
                CarrierServiceReference = "CSR",
                CarrierServiceName = "CarrierServiceName",
                CarrierName = "CarrierName",
                ConsignmentReferenceProvidedByCustomer = "ConsignmentReferenceProvidedByCustomer",
                ConsignmentReferenceForAllLegsAssignedByMpd = "EC-848-778-213",
                CarrierServiceCode = "CarrierServiceCode",
                CarrierEventDateTime = new DateTimeOffset(new DateTime(2019, 02, 03, 04, 05, 06), new TimeSpan(1, 0, 0)),
                SortedReceivedDate = new DateTimeOffset(new DateTime(2019, 02, 03, 04, 05, 06), new TimeSpan(1, 0, 0)),
            };

            var config = new MassTransitConfiguration();

            var busControl = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                var serviceUri =
                    ServiceBusEnvironment.CreateServiceUri("sb", config.AzureNamespace, config.AzureQueueName);

                cfg.Host(serviceUri, h =>
                {
                    h.TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(
                        config.AzureKeyName,
                        config.AzureKey);
                    h.TransportType = TransportType.NetMessaging;
                    h.OperationTimeout = new TimeSpan(0, 0, 30);
                });

                cfg.Publish<ConsignmentPackageTrackingEvent>(pcfg => { pcfg.EnableBatchedOperations = true; });
            });

            //var bus = await busControl.StartAsync();
            await busControl.Publish(message);

        }
    }
}