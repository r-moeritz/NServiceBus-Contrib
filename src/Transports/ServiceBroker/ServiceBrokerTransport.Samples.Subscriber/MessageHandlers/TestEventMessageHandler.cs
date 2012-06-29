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
            Logger.DebugFormat("Received message of type 'ITestEvent' with content: '{0}'",
                               message.Content);
        }
    }
}
