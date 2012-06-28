using System.Configuration;

namespace NServiceBus.Config
{
    public class ServiceBrokerTransportConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue to receive messages from in the format
        /// "[database].[schema].[queue]".
        /// </summary>
        [ConfigurationProperty("InputQueue", IsRequired = true)]
        public string InputQueue
        {
            get { return this["InputQueue"] as string; }
            set { this["InputQueue"] = value; }
        }

        /// <summary>
        /// The number of seconds to wait while polling for messages.
        /// </summary>
        [ConfigurationProperty("SecondsToWaitForMessage", IsRequired = true)]
        public int SecondsToWaitForMessage
        {
            get { return (int)this["SecondsToWaitForMessage"]; }
            set { this["SecondsToWaitForMessage"] = value; }
        }

        [ConfigurationProperty("ConnectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return (string) this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }
    }
}