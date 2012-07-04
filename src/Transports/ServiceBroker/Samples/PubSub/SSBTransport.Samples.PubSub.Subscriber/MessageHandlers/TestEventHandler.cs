using NServiceBus;
using SSBTransport.Samples.Common.Events;
using log4net;

namespace SSBTransport.Samples.PubSub.Subscriber.MessageHandlers
{
    public class TestEventHandler : IHandleMessages<ITestEvent>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TestEventHandler));

        public void Handle(ITestEvent message)
        {
            Logger.InfoFormat("Received message '{0}' from MSMQ", message.Content);
        }
    }
}
