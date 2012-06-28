using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Transport.ServiceBroker.Config
{
    public class ConfigServiceBrokerTransport : Configure
    {
        private IComponentConfig<ServiceBrokerMessageReceiver> _receiverConfig;
        private IComponentConfig<ServiceBrokerMessageSender> _senderConfig;

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
                Configurer.ConfigureComponent<ServiceBrokerMessageSender>(DependencyLifecycle.SingleInstance);

            var cfg = GetConfigSection<ServiceBrokerTransportConfig>();
            if (cfg == null) return;

            InputQueue(cfg.InputQueue);
            ConnectionString(cfg.ConnectionString);
            SecondsToWaitForMessage(cfg.SecondsToWaitForMessage);
        }

        public new IStartableBus CreateBus()
        {
            var bus = (UnicastBus) base.CreateBus();
            bus.MessageSender = Builder.Build<ServiceBrokerMessageSender>();
            return bus;
        }

        public ConfigServiceBrokerTransport InputQueue(string value)
        {
            _receiverConfig.ConfigureProperty(t => t.InputQueue, value);
            return this;
        }

        public ConfigServiceBrokerTransport ConnectionString(string value)
        {
            _receiverConfig.ConfigureProperty(t => t.ConnectionString, value);
            _senderConfig.ConfigureProperty(t => t.ConnectionString, value);
            return this;
        }

        public ConfigServiceBrokerTransport SecondsToWaitForMessage(int value)
        {
            _receiverConfig.ConfigureProperty(t => t.SecondsToWaitForMessage, value);
            return this;
        }
    }
}