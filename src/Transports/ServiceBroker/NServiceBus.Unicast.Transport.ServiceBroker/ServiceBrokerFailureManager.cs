using System;
using NServiceBus.Faults;
using NServiceBus.Unicast.Transport.ServiceBroker.Util;
using ServiceBroker.Net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerFailureManager : IManageMessageFailures
    {
        /// <summary>
        /// SSB service configured to receive error messages.
        /// </summary>
        private string _errorService;

        /// <summary>
        /// SSB Service configured as the conversation initiator if none present in message headers.
        /// </summary>
        public string InitiatorService { get; set; }

        /// <summary>
        /// Sql connection string to the service hosting the service broker
        /// </summary>
        public string ConnectionString { get; set; }

        private void MoveToErrorService(TransportMessage message)
        {
            string initiator;
            if (!message.Headers.TryGetValue(ServiceBrokerTransportHeaderKeys.InitiatorService, out initiator))
                initiator = InitiatorService;

            new ServiceBrokerTransactionManager(ConnectionString).RunInTransaction(
                xaction =>
                    {
                        var handle = ServiceBrokerWrapper.BeginConversation(xaction, initiator, _errorService,
                                                                            Constants.NServiceBusTransportMessageContract);
                        ServiceBrokerWrapper.Send(xaction, handle, Constants.NServiceBusTransportMessage, message.Body);
                        ServiceBrokerWrapper.ForceEndConversation(xaction, handle);
                    });
        }

        public void Init(Address address)
        {
            if (String.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("Connection string must be provided");

            if (address == null)
                throw new ArgumentException("Error service must be specified");

            if (String.IsNullOrEmpty(address.Queue))
                throw new ArgumentException("Error service must not be null or an empty string");

            ConnectionString.TestConnection();

            _errorService = address.Queue;
        }

        public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
        {
            MoveToErrorService(message);
        }

        public void SerializationFailedForMessage(TransportMessage message, Exception e)
        {
            MoveToErrorService(message);
        }
    }
}
