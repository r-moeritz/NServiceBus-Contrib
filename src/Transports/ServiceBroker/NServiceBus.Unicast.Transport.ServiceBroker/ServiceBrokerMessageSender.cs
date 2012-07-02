using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport.ServiceBroker.Util;
using ServiceBroker.Net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerMessageSender : ISendMessages
    {
        /// <summary>
        /// The name of the SSB service configured as the conversation initiator.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// SSB Service configured as the conversation initiator.
        /// </summary>
        public string InitiatorService { get; set; }        

        private static string SerializeToXml(TransportMessage transportMessage)
        {
            var overrides = new XmlAttributeOverrides();
            var attrs = new XmlAttributes {XmlIgnore = true};

            // Exclude non-serializable members
            overrides.Add(typeof (TransportMessage), "Body", attrs);
            overrides.Add(typeof (TransportMessage), "ReplyToAddress", attrs);
            overrides.Add(typeof (TransportMessage), "Headers", attrs);
            
            var sb = new StringBuilder();
            var xws = new XmlWriterSettings { Encoding = Encoding.Unicode };
            var xw = XmlWriter.Create(sb, xws);
            var xs = new XmlSerializer(typeof (TransportMessage), overrides);
            xs.Serialize(xw, transportMessage);

            var xdoc = XDocument.Parse(sb.ToString());
            var body = new XElement("Body");
            var cdata = new XCData(Encoding.UTF8.GetString(transportMessage.Body));
            body.Add(cdata);
            xdoc.SafeElement("TransportMessage").Add(body);

            sb.Clear();
            var sw = new StringWriter(sb);
            xdoc.Save(sw);

            return sb.ToString();
        }

        /// <summary>
        /// Sends a message to the specified destination.
        /// </summary>
        /// <param name="m">The message to send.</param>
        /// <param name="destination">The address of the destination to send the message to.</param>
        public void Send(TransportMessage m, Address destination)
        {
            string initiator;
            if (!m.Headers.TryGetValue(ServiceBrokerTransportHeaderKeys.InitiatorService, out initiator))
                initiator = InitiatorService;

            new ServiceBrokerTransactionManager(ConnectionString).RunInTransaction(
                transaction =>
                    {
                        // Always begin and end a conversation to simulate a monologe
                        var conversationHandle =
                            ServiceBrokerWrapper.
                                BeginConversation(
                                    transaction,
                                    initiator,
                                    destination.ToString(),
                                    Constants.NServiceBusTransportMessageContract);

                        // Use the conversation handle as the message Id
                        m.Id = conversationHandle.ToString();

                        // Set the time from the source machine when the message was sent
                        m.SetHeader("TimeSent", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

                        // Serialize the transport message
                        var xml = SerializeToXml(m);

                        ServiceBrokerWrapper.Send(
                            transaction,
                            conversationHandle,
                            Constants.NServiceBusTransportMessage,
                            Encoding.Unicode.GetBytes(xml));

                        ServiceBrokerWrapper.EndConversation(transaction,
                                                             conversationHandle);
                    });
        }
    }
}