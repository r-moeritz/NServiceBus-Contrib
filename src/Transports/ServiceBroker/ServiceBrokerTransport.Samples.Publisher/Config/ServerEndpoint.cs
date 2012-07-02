using System;
using System.Threading;
using NServiceBus;
using ServiceBrokerTransport.Samples.Events;
using log4net;

namespace ServiceBrokerTransport.Samples.Publisher.Config
{
    public class ServerEndpoint : IWantToRunAtStartup
    {
        private const int PeriodMs = 5000;
        private static readonly ILog Logger = LogManager.GetLogger("Logger");
        
        public IBus Bus { get; set; }

        private void SendMessage()
        {
            Logger.Debug("Sending message...");
            var evt = Bus.CreateInstance<ITestEvent>(
                @event => @event.Content = "Event generated at " + DateTime.UtcNow);
            Bus.Send(Constants.ServiceName, evt);
            Logger.Debug("Message sent!");
        }

        public void Run()
        {
           new Timer(_ => SendMessage(), null, PeriodMs, PeriodMs);
        }

        public void Stop()
        {
        }
    }
}