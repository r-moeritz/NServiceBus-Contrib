using NServiceBus;
using SSBTransport.Samples.Common;

namespace TestMessageSubscriber
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .Log4Net()
                .DefiningEventsAs(Functions.IsEvent)
                .DisableTimeoutManager()
                .MsmqTransport();
        }
    }
}
