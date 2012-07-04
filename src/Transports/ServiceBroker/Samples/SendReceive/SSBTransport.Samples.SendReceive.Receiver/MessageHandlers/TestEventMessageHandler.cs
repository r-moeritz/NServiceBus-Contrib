using NServiceBus;
using NServiceBus.Unicast.Transport.ServiceBroker;
using SSBTransport.Samples.Common.Events;
using log4net;

namespace SSBTransport.Samples.SendReceive.Receiver.MessageHandlers
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
