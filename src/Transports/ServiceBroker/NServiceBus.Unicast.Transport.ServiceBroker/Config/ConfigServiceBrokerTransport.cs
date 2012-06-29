using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.Msmq;

namespace NServiceBus.Unicast.Transport.ServiceBroker.Config
{
    public class ConfigServiceBrokerTransport : Configure
    {
        private IComponentConfig<ServiceBrokerMessageReceiver> _receiverConfig;
        private IComponentConfig<MsmqMessageSender> _senderConfig;

        /// <summary>
        /// Wraps the given configuration object but stores the same 
        /// builder and configurer properties.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;

            _receiverConfig =
                Configurer.ConfigureComponent<ServiceBrokerMessageReceiver>(DependencyLifecycle.SingleInstance);
            _senderConfig =
                Configurer.ConfigureComponent<MsmqMessageSender>(DependencyLifecycle.SingleInstance);

            var ssbConfig = GetConfigSection<ServiceBrokerTransportConfig>();
            if (ssbConfig == null) return;

            ConnectionString(ssbConfig.ConnectionString);
            SecondsToWaitForMessage(ssbConfig.SecondsToWaitForMessage);
        }

        public ConfigServiceBrokerTransport ConnectionString(string value)
        {
            _receiverConfig.ConfigureProperty(t => t.ConnectionString, value);
            return this;
        }

        public ConfigServiceBrokerTransport SecondsToWaitForMessage(int value)
        {
            _receiverConfig.ConfigureProperty(t => t.SecondsToWaitForMessage, value);
            return this;
        }
    }
}