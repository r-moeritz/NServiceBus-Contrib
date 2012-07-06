using System.Configuration;

namespace NServiceBus.Config
{
    public class ServiceBrokerTransportConfig : ConfigurationSection
    {
        /// <summary>
        /// Sets the maximum interval, in seconds, that a thread
        /// waits to receive messages from the queue before giving up.
        /// 
        /// The default value is 10.
        /// </summary>
        [ConfigurationProperty("SecondsToWaitForMessage", IsRequired = false)]
        public int SecondsToWaitForMessage
        {
            get { return (int) this["SecondsToWaitForMessage"]; }
            set { this["SecondsToWaitForMessage"] = value; }
        }

        /// <summary>
        /// The connection string used to connect to SSB.
        /// </summary>
        [ConfigurationProperty("ConnectionString", IsRequired = false)]
        public string ConnectionString
        {
            get { return (string) this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }

        /// <summary>
        /// SSB Service configured as the conversation initiator if none present in message headers.
        /// </summary>
        [ConfigurationProperty("InitiatorService", IsRequired = false)]
        public string InitiatorService
        {
            get { return (string) this["InitiatorService"]; }
            set { this["InitiatorService"] = value; }
        }
    }
}