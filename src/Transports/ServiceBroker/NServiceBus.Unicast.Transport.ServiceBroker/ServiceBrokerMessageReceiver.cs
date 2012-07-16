using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Transport.ServiceBroker.Util;
using ServiceBroker.Net;
using log4net;

namespace NServiceBus.Unicast.Transport.ServiceBroker
{
    public class ServiceBrokerMessageReceiver : IReceiveMessages
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (ServiceBrokerMessageReceiver));

        /// <summary>
        /// The default number of seconds to wait for a message
        /// to appear in the input queue before giving up. This
        /// value will be used if none is configured.
        /// </summary>
        private const int DefaultSecondsToWaitForMessage = 10;

        /// <summary>
        /// The default number of messages to be retrieved in a
        /// single RECEIVE statement. This value will be used
        /// if none is configured.
        /// </summary>
        private const int DefaultReceiveBatchSize = 50;

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

        public int? SecondsToWaitForMessage
        {
            get { return _secondsToWaitForMessage ?? DefaultSecondsToWaitForMessage; }
            set { _secondsToWaitForMessage = value; }
        }

        /// <summary>
        /// Sets the maximum number of messages to be retrieved in
        /// a single RECEIVE statement.
        /// </summary>
        private int? _receiveBatchSize;

        public int? ReceiveBatchSize
        {
            get { return _receiveBatchSize ?? DefaultReceiveBatchSize; }
            set { _receiveBatchSize = value; }
        }

        private static TransportMessage ExtractTransportMessage(IEnumerable<Message> messages)
        {
            var tspMessages = messages.Where(m => m.MessageTypeName == Constants.NServiceBusTransportMessage).ToArray();
            Logger.DebugFormat("Got {0} messages from SSB. Going to extract into 1 transport message.", tspMessages.Length);

            var messagesDoc = new XDocument(new XElement("Messages"));
            foreach (var msg in tspMessages)
            {
                var doc = XDocument.Load(msg.BodyStream);
                var cdata = (XCData) doc.SafeElement("TransportMessage").SafeElement("Body").FirstNode;

                var messagesElement = XElement.Parse(cdata.Value);
                var msgElement = messagesElement.FirstNode;

                messagesDoc.SafeElement("Messages").Add(msgElement);
            }

            var messagesXml = messagesDoc.SafeElement("Messages").ToString();
            Logger.Debug(messagesXml);

            var messagesCData = new XCData(messagesXml);
            var transportMessageId = Guid.NewGuid().ToString();

            return new TransportMessage
                       {
                           Id = transportMessageId,
                           IdForCorrelation = transportMessageId,
                           Headers = new Dictionary<string, string>
                                         {
                                             {
                                                 ServiceBrokerTransportHeaderKeys.UtcTimeReceived,
                                                 DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                                             }
                                         },
                           Body = Encoding.Unicode.GetBytes(messagesCData.Value)
                       };
        }

        private TransportMessage ReceiveFromQueue(IDbTransaction transaction)
        {
            var ssbMessages = ServiceBrokerWrapper.WaitAndReceive(transaction, _inputQueue, 
                SecondsToWaitForMessage.GetValueOrDefault()*1000, ReceiveBatchSize.GetValueOrDefault());
            if (ssbMessages == null) return null;

            var transportMessage = ExtractTransportMessage(ssbMessages);
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
        }

        public bool HasMessage()
        {
            return true;
        }

        public TransportMessage Receive()
        {
            TransportMessage message = null;
            new ServiceBrokerTransactionManager(ConnectionString)
                .RunInTransaction(x => { message = ReceiveFromQueue(x); });
            return message;
        }
    }
}