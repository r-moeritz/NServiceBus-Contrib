using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport.ServiceBroker.Util;
using ServiceBroker.Net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerMessageReceiver : IReceiveMessages
    {
        /// <summary>
        /// The default number of seconds to wait for a message
        /// to appear in the input queue before giving up. This
        /// value will be used if none is configured.
        /// </summary>
        private const int DefaultSecondsToWaitForMessage = 10;

        private ServiceBrokerTransactionManager _transactionManager;

        /// <summary>
        /// The path to the SSB queue the receiver will read from.
        /// </summary>
        private string _inputQueue;

        /// <summary>
        /// The connection string used to connect to SSB.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Sets the maximum interval, in seconds, that a thread waits 
        /// to receive messages from the input queue before giving up.
        /// </summary>
        private int? _secondsToWaitForMessage;
        public int SecondsToWaitForMessage
        {
          get
          {
            return _secondsToWaitForMessage ?? DefaultSecondsToWaitForMessage;
          }
          set
          {
            _secondsToWaitForMessage = value;
          }
        }
        
        private static TransportMessage DeserializeTransportMessage(Stream stream)
        {
            stream.Position = 0;

            var overrides = new XmlAttributeOverrides();
            var attrs = new XmlAttributes {XmlIgnore = true};

            // Exclude non-serializable members
            overrides.Add(typeof (TransportMessage), "Body", attrs);
            overrides.Add(typeof (TransportMessage), "ReplyToAddress", attrs);
            overrides.Add(typeof (TransportMessage), "Headers", attrs);

            var xs = new XmlSerializer(typeof (TransportMessage), overrides);
            var transportMessage = (TransportMessage) xs.Deserialize(stream);
            return transportMessage;
        }

        private static TransportMessage ExtractXmlTransportMessage(Stream stream)
        {
            stream.Position = 0;

            var xdoc = XDocument.Load(stream);
            var payload = (XCData) xdoc.SafeElement("TransportMessage").SafeElement("Body").FirstNode;

            var transportMessage = DeserializeTransportMessage(stream);
            transportMessage.Headers = new Dictionary<string, string>
                                           {
                                               {
                                                   ServiceBrokerTransportHeaderKeys.UtcTimeReceived,
                                                   DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                                               }
                                           };
            transportMessage.Body = Encoding.Unicode.GetBytes(payload.Value);

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
            if (String.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("Connection string must be provided");

            if (address == null)
                throw new ArgumentException("Input queue must be specified");

            if (String.IsNullOrEmpty(address.Queue))
                throw new ArgumentException("Input queue must not be null or an empty string");

            ConnectionString.TestConnection();

            _inputQueue = address.Queue;

            _transactionManager = new ServiceBrokerTransactionManager(ConnectionString);
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