using NServiceBus;

namespace ServiceBrokerTransport.Samples.Subscriber.Config
{
    [EndpointName("ServiceAQueueSubscriber")]
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .Log4Net()
                .DefiningEventsAs(type => type.Namespace != null
                                          && type.Namespace.EndsWith(".Events")
                                          && type.Name.EndsWith("Event"))
                .DefaultBuilder()
                .UseInMemoryTimeoutPersister()
                .ServiceBrokerTransport()
                .InputQueue("ServiceAQueue")
                .ConnectionString(@"Server=.;Database=ServiceBroker_HelloWorld;Integrated Security=True");
        }
    }
}
