using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;
using NServiceBus.Unicast.Queuing;
using ServiceBroker.Net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerMessageReceiver : IReceiveMessages
    {
        private SqlServiceBrokerTransactionManager _transactionManager;

        /// <summary>
        /// Sql connection string to the service hosting the service broker
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The path to the queue the transport will read from.
        /// </summary>
        public string InputQueue { get; set; }

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
             * We currently only retrieve the message body because TransportMessage can't 
             * be deserialized by the XmlSerializer as it lacks a parameterless ctor.
             */

            bodyStream.Position = 0;

            var bodyDoc = new XmlDocument();
            bodyDoc.Load(bodyStream);

            var payLoad = bodyDoc.DocumentElement.SelectSingleNode("Body").FirstChild as XmlCDataSection;

            var transportMessage = new TransportMessage { Body = Encoding.Unicode.GetBytes(payLoad.Data) };
            return transportMessage;
        }

        private TransportMessage ReceiveFromQueue(SqlTransaction transaction)
        {
            var message = ServiceBrokerWrapper.WaitAndReceive(transaction, InputQueue, SecondsToWaitForMessage*1000);

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
                        count = ServiceBrokerWrapper.QueryMessageCount(transaction, InputQueue,
                                                                       Constants.NServiceBusTransportMessage);
                    });
            return count;
        }
    }
}