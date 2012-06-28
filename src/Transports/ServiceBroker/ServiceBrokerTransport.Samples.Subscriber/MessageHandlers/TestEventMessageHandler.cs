using NServiceBus;
using ServiceBrokerTransport.Samples.Subscriber.Events;
using log4net;

namespace ServiceBrokerTransport.Samples.Subscriber.MessageHandlers
{
    public class TestEventMessageHandler : IHandleMessages<ITestEvent>
    {
        private static readonly ILog Logger = LogManager.GetLogger("Logger");

        public void Handle(ITestEvent message)
        {
            Logger.DebugFormat("Received message of type '{0}' with content: '{1}'",
                               message.GetType(), message.Content);
        }
    }
}
