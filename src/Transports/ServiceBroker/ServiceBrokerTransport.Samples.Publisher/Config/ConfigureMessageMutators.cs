using NServiceBus;

namespace ServiceBrokerTransport.Samples.Publisher.Config
{
    public class ConfigureMessageMutators : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<TransportMessageMutator>(
                DependencyLifecycle.InstancePerCall);
        }
    }
}
