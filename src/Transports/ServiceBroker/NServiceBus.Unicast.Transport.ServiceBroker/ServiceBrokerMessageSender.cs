using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NServiceBus.Unicast.Queuing;
using ServiceBroker.Net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerMessageSender : ISendMessages
    {
        /// <summary>
        /// Sql connection string to the service hosting the service broker
        /// </summary>
        public string ConnectionString { get; set; }

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
            new SqlServiceBrokerTransactionManager(ConnectionString).RunInTransaction(
                transaction =>
                    {
                        // Always begin and end a conversation to simulate a monologe
                        var conversationHandle =
                            ServiceBrokerWrapper.
                                BeginConversation(
                                    transaction,
                                    m.ReplyToAddress.ToString(),
                                    destination.ToString(),
                                    Constants.NServiceBusTransportMessageContract);

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
                                Constants.NServiceBusTransportMessage,
                                stream.GetBuffer());
                        }
                        ServiceBrokerWrapper.EndConversation
                            (transaction, conversationHandle);
                    });
        }
    }
}