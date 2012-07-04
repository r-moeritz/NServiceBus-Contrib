using NServiceBus;
using SSBTransport.Samples.Common;

namespace ServiceBQueue
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .Log4Net()
                .DefiningEventsAs(Functions.IsEvent)
                .DisableTimeoutManager()
                .ServiceBrokerTransport()
                .InitiatorService("ServiceB")
                .ConnectionString(Constants.ConnectionString);
        }
    }
}
