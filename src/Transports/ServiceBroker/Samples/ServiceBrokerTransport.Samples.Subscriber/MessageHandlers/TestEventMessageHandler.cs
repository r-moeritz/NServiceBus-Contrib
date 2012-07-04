using NServiceBus;
using NServiceBus.Unicast.Transport.ServiceBroker;
using ServiceBrokerTransport.Samples.Events;
using log4net;

namespace ServiceBrokerTransport.Samples.Subscriber.MessageHandlers
{
    public class TestEventMessageHandler : IHandleMessages<ITestEvent>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TestEventMessageHandler));

        public void Handle(ITestEvent message)
        {
            var timeReceived = message.GetHeader(ServiceBrokerTransportHeaderKeys.UtcTimeReceived);
            Logger.DebugFormat("Received message '{0}' at '{1}'", message.Content, timeReceived);
        }
    }
}
