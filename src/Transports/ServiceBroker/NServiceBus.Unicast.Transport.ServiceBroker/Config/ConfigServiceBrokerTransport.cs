using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Transport.ServiceBroker.Config
{
    public class ConfigServiceBrokerTransport : Configure
    {
        private IComponentConfig<ServiceBrokerMessageReceiver> _receiverConfig;
        private IComponentConfig<ServiceBrokerMessageSender> _senderConfig;
        private IComponentConfig<ServiceBrokerFailureManager> _failureConfig;

        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;

            _receiverConfig =
                Configurer.ConfigureComponent<ServiceBrokerMessageReceiver>(DependencyLifecycle.SingleInstance);
            _senderConfig =
                Configurer.ConfigureComponent<ServiceBrokerMessageSender>(DependencyLifecycle.SingleInstance);
            _failureConfig =
                Configurer.ConfigureComponent<ServiceBrokerFailureManager>(DependencyLifecycle.SingleInstance);

            var cfg = GetConfigSection<ServiceBrokerTransportConfig>();
            if (cfg == null) return;

            ConnectionString(cfg.ConnectionString);
            SecondsToWaitForMessage(cfg.SecondsToWaitForMessage);
            InitiatorService(cfg.InitiatorService);
            ReceiveBatchSize(cfg.ReceiveBatchSize);
            EndConversationAfterReceive(cfg.EndConversationAfterReceive);
        }

        public ConfigServiceBrokerTransport ReceiveBatchSize(int? value)
        {
            _receiverConfig.ConfigureProperty(t => t.ReceiveBatchSize, value);
            return this;
        }

        public ConfigServiceBrokerTransport ConnectionString(string value)
        {
            _receiverConfig.ConfigureProperty(t => t.ConnectionString, value);
            _senderConfig.ConfigureProperty(t => t.ConnectionString, value);
            _failureConfig.ConfigureProperty(t => t.ConnectionString, value);
            return this;
        }

        public ConfigServiceBrokerTransport SecondsToWaitForMessage(int? value)
        {
            _receiverConfig.ConfigureProperty(t => t.SecondsToWaitForMessage, value);
            return this;
        }

        public ConfigServiceBrokerTransport InitiatorService(string value)
        {
            _senderConfig.ConfigureProperty(t => t.InitiatorService, value);
            _failureConfig.ConfigureProperty(t => t.InitiatorService, value);
            return this;
        }

        public ConfigServiceBrokerTransport EndConversationAfterReceive(bool? value)
        {
            _receiverConfig.ConfigureProperty(t => t.EndConversationAfterReceive, value);
            return this;
        }
    }
}