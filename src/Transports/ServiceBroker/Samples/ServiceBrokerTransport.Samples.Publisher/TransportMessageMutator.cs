using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.ServiceBroker;

namespace ServiceBrokerTransport.Samples.Publisher
{
    public class TransportMessageMutator : IMutateTransportMessages
    {
        public void MutateIncoming(TransportMessage transportMessage)
        {
        }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers.Add(ServiceBrokerTransportHeaderKeys.InitiatorService, Constants.ServiceName);
        }
    }
}
