using System;
using System.Threading;
using NServiceBus;
using SSBTransport.Samples.Common.Events;
using log4net;

namespace SSBTransport.Samples.SendReceive.Sender
{
    public class ServerEndpoint : IWantToRunAtStartup
    {
        private const int PeriodMs = 5000;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerEndpoint));
        
        public IBus Bus { get; set; }

        private void SendMessage()
        {
            Logger.Debug("Sending message...");
            var evt = Bus.CreateInstance<ITestEvent>(
                @event => @event.Content = "Event generated at " + DateTime.UtcNow);
            Bus.Send("ServiceB", evt);
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