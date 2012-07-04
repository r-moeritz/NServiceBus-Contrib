using NServiceBus;
using NServiceBus.Unicast.Transport.ServiceBroker;
using SSBTransport.Samples.Common;

namespace ServiceAQueue
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .Log4Net()
                .DefiningEventsAs(Functions.IsEvent)
                .DisableTimeoutManager()
                .MsmqTransport();

            // Register the ServiceBrokerMessageReceiver so we can receive messages from SSB.
            Configure.Instance.Configurer
                .ConfigureComponent<ServiceBrokerMessageReceiver>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.ConnectionString, Constants.ConnectionString);
        }
    }
}
