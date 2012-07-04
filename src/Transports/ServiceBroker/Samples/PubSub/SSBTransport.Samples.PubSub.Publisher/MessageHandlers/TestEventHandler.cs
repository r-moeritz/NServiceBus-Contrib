using NServiceBus;
using SSBTransport.Samples.Common.Events;
using log4net;

namespace SSBTransport.Samples.PubSub.Publisher.MessageHandlers
{
    public class TestEventHandler : IHandleMessages<ITestEvent>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (TestEventHandler));

        public IBus Bus { get; set; }

        public void Handle(ITestEvent message)
        {
            Logger.InfoFormat("Received message '{0}' from SSB; going to publish...", message.Content);
            Bus.Publish(message);
            Logger.Info("Message published!");
        }
    }
}
