using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml.Linq;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport.ServiceBroker.Util;
using ServiceBroker.Net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerMessageReceiver : IReceiveMessages
    {
        private SqlServiceBrokerTransactionManager _transactionManager;

        /// <summary>
        /// The path to the queue the transport will read from.
        /// </summary>
        private string _inputQueue;

        /// <summary>
        /// Sql connection string to the service hosting the service broker
        /// </summary>
        public string ConnectionString { get; set; }
                
        /// <summary>
        /// Sets the maximum interval of time for when a thread thinks there is a message in the queue
        /// that it tries to receive, until it gives up.
        /// 
        /// Default value is 10.
        /// </summary>
        public int SecondsToWaitForMessage { get; set; }

        private void VerifyConnection()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
            }
        }

        private static TransportMessage ExtractXmlTransportMessage(Stream bodyStream)
        {
            /*
             * TODO:
             * We currently only retrieve the message body because TransportMessage.ReplyToAddress
             * is of type NServiceBus.Address which can't be serialized because it doesn't have a
             * parameterless ctor.
             */

            bodyStream.Position = 0;

            var bodyDoc = XDocument.Load(bodyStream);

            var id = bodyDoc.SafeElement("TransportMessage").SafeElement("Id").Value;
            var payload = (XCData) bodyDoc.SafeElement("TransportMessage").SafeElement("Body").FirstNode;

            var transportMessage = new TransportMessage
                                       {
                                           Id = id,
                                           Headers = new Dictionary<string, string>(),
                                           Body = Encoding.Unicode.GetBytes(payload.Value)
                                       };
            return transportMessage;
        }

        private TransportMessage ReceiveFromQueue(IDbTransaction transaction)
        {
            var message = ServiceBrokerWrapper.WaitAndReceive(transaction, _inputQueue, SecondsToWaitForMessage*1000);

            // No message? That's okay
            if (message == null)
                return null;

            // Only handle transport messages
            if (message.MessageTypeName != Constants.NServiceBusTransportMessage)
                return null;

            var transportMessage = ExtractXmlTransportMessage(message.BodyStream);

            // Set the correlation Id
            if (string.IsNullOrEmpty(transportMessage.IdForCorrelation))
                transportMessage.IdForCorrelation = transportMessage.Id;

            return transportMessage;
        }

        public void Init(Address address, bool transactional)
        {
            VerifyConnection();

            if (address == null)
                throw new ArgumentException("Input queue must be specified");

            if (String.IsNullOrEmpty(address.Queue))
                throw new ArgumentException("Input queue must not be null or an empty string");

            _inputQueue = address.Queue;

            _transactionManager = new SqlServiceBrokerTransactionManager(ConnectionString);
        }

        public bool HasMessage()
        {
            return GetNumberOfPendingMessages() > 0;
        }

        public TransportMessage Receive()
        {
            TransportMessage message = null;
            _transactionManager.RunInTransaction(x => { message = ReceiveFromQueue(x); });
            return message;
        }

        private int GetNumberOfPendingMessages()
        {
            var count = -1;
            _transactionManager.RunInTransaction(
                transaction =>
                    {
                        count = ServiceBrokerWrapper.QueryMessageCount(transaction, _inputQueue,
                                                                       Constants.NServiceBusTransportMessage);
                    });
            return count;
        }
    }
}