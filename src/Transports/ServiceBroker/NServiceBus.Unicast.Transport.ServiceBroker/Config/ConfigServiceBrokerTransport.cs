using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Transport.ServiceBroker.Config
{
    public class ConfigServiceBrokerTransport : Configure
    {
        /// <summary>
        /// Wraps the given configuration object but stores the same 
        /// builder and configurer properties.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;

            transportConfig =
                Configurer.ConfigureComponent<ServiceBrokerMessageReceiver>(DependencyLifecycle.SingleInstance);

            var cfg = GetConfigSection<ServiceBrokerTransportConfig>();
            if (cfg == null) return;

            transportConfig.ConfigureProperty(t => t.InputQueue, cfg.InputQueue);
            ConnectionString(cfg.ConnectionString);
        }

        private IComponentConfig<ServiceBrokerMessageReceiver> transportConfig;

        public ConfigServiceBrokerTransport ConnectionString(string value)
        {
            transportConfig.ConfigureProperty(t => t.ConnectionString, value);
            return this;
        }

        public ConfigServiceBrokerTransport ErrorService(string value)
        {
            //TODO
            //transportConfig.ConfigureProperty(t => t.ErrorService, value);
            return this;
        }

        public ConfigServiceBrokerTransport SecondsToWaitForMessage(int value)
        {
            transportConfig.ConfigureProperty(t => t.SecondsToWaitForMessage, value);
            return this;
        }
    }
}