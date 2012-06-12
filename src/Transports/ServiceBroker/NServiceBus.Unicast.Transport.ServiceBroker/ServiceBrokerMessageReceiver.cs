using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NServiceBus.Unicast.Queuing;
using ServiceBroker.Net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerMessageReceiver : IReceiveMessages
    {
        public const string NServiceBusTransportMessageContract = "NServiceBusTransportMessageContract";
        public const string NServiceBusTransportMessage = "NServiceBusTransportMessage";

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

        private TransportMessage ExtractXmlTransportMessage(Stream bodyStream)
        {
            var xs = new XmlSerializer(typeof (TransportMessage));
            var transportMessage = (TransportMessage) xs.Deserialize(bodyStream);

            bodyStream.Position = 0;

            var bodyDoc = new XmlDocument();
            bodyDoc.Load(bodyStream);

            var payLoad = bodyDoc.DocumentElement.SelectSingleNode("Body").FirstChild as XmlCDataSection;

            transportMessage.Body = Encoding.UTF8.GetBytes(payLoad.Data);

            return transportMessage;
        }

        private TransportMessage ReceiveFromQueue(SqlTransaction transaction)
        {
            var message = ServiceBrokerWrapper.WaitAndReceive(transaction, InputQueue, SecondsToWaitForMessage*1000);

            // No message? That's okay
            if (message == null)
                return null;

            // Only handle transport messages
            if (message.MessageTypeName != NServiceBusTransportMessage)
                return null;

            TransportMessage transportMessage = null;

            transportMessage = ExtractXmlTransportMessage(message.BodyStream);

            // Set the correlation Id
            if (string.IsNullOrEmpty(transportMessage.IdForCorrelation))
                transportMessage.IdForCorrelation = transportMessage.Id;

            return transportMessage;
        }

        private SqlServiceBrokerTransactionManager transactionManager;

        public void Init(Address address, bool transactional)
        {
            VerifyConnection();

            transactionManager = new SqlServiceBrokerTransactionManager(ConnectionString);
        }

        public bool HasMessage()
        {
            return GetNumberOfPendingMessages() > 0;
        }

        public TransportMessage Receive()
        {
            TransportMessage message = null;
            transactionManager.RunInTransaction(x => { message = ReceiveFromQueue(x); });
            return message;
        }

        private int GetNumberOfPendingMessages()
        {
            var count = -1;
            transactionManager.RunInTransaction(
                transaction =>
                    {
                        count = ServiceBrokerWrapper.QueryMessageCount(transaction, InputQueue,
                                                                       NServiceBusTransportMessage);
                    });
            return count;
        }
    }

    public class ServiceBrokerMessageSender : ISendMessages
    {
        public const string NServiceBusTransportMessageContract = "NServiceBusTransportMessageContract";
        public const string NServiceBusTransportMessage = "NServiceBusTransportMessage";

        private static void SerializeToXml(TransportMessage transportMessage, Stream stream)
        {
            var overrides = new XmlAttributeOverrides();
            var attrs = new XmlAttributes {XmlIgnore = true};

            overrides.Add(typeof (TransportMessage), "Messages", attrs);
            var xs = new XmlSerializer(typeof (TransportMessage), overrides);

            var doc = new XmlDocument();

            using (var tempstream = new MemoryStream())
            {
                xs.Serialize(tempstream, transportMessage);
                tempstream.Position = 0;

                doc.Load(tempstream);
            }

            var bodyElement = doc.CreateElement("Body");

            var data = Encoding.Unicode.GetString(transportMessage.Body);

            bodyElement.AppendChild(doc.CreateCDataSection(data));
            doc.DocumentElement.AppendChild(bodyElement);

            doc.Save(stream);
            stream.Position = 0;
        }

        /// <summary>
        /// Sends a message to the specified destination.
        /// </summary>
        /// <param name="m">The message to send.</param>
        /// <param name="destination">The address of the destination to send the message to.</param>
        public void Send(TransportMessage m, Address destination)
        {
            new SqlServiceBrokerTransactionManager("TODO").RunInTransaction(
                transaction =>
                    {
                        // Always begin and end a conversation to simulate a monologe
                        var conversationHandle =
                            ServiceBrokerWrapper.
                                BeginConversation(
                                    transaction,
                                    m.ReplyToAddress.ToString(),
                                    destination.ToString(),
                                    NServiceBusTransportMessageContract);

                        // Use the conversation handle as the message Id
                        m.Id = conversationHandle.ToString();

                        // Set the time from the source machine when the message was sent
                        m.SetHeader("TimeSent", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

                        using (var stream = new MemoryStream())
                        {
                            // Serialize the transport message
                            SerializeToXml(m, stream);


                            ServiceBrokerWrapper.Send(
                                transaction,
                                conversationHandle,
                                NServiceBusTransportMessage,
                                stream.GetBuffer());
                        }
                        ServiceBrokerWrapper.EndConversation
                            (transaction, conversationHandle);
                    });
        }
    }
}