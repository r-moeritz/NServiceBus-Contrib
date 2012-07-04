using NServiceBus;
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
                .ServiceBrokerTransport()
                .InitiatorService("ServiceA")
                .ConnectionString(Constants.ConnectionString);
        }
    }
}
