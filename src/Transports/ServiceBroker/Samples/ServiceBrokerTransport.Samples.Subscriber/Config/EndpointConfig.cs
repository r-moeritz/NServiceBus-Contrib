using NServiceBus;

namespace ServiceAQueue
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .Log4Net()
                .DefiningEventsAs(type => type.Namespace != null
                                          && type.Namespace.EndsWith(".Events")
                                          && type.Name.EndsWith("Event"))
                .DisableTimeoutManager()
                .ServiceBrokerTransport()
                .InitiatorService("ServiceA")
                .ConnectionString(@"Server=.;Database=ServiceBroker_HelloWorld;Integrated Security=True");
        }
    }
}
