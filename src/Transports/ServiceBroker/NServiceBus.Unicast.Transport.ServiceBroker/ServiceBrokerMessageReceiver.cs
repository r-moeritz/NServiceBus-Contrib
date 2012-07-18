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
            if (tspMessages.Length == 0) return null;

            var tempCData = (XCData) XDocument.Load(tspMessages.First().BodyStream)
                                         .SafeElement("TransportMessage")
                                         .SafeElement("Body")
                                         .FirstNode;
            var ns = XElement.Parse(tempCData.Value).GetDefaultNamespace();
            var root = new XElement(ns.GetName("Messages"));
            
            foreach (var msgElement in tspMessages.Select(
                msg => XDocument.Load(msg.BodyStream))
                .Select(doc => (XCData) doc.SafeElement("TransportMessage")
                                            .SafeElement("Body").FirstNode)
                .Select(cdata => XElement.Parse(cdata.Value))
                .SelectMany(container => container.Elements()))
            {
                root.Add(msgElement);
            }

            Logger.Debug(Environment.NewLine + root);
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
                           Body = Encoding.Unicode.GetBytes(root.ToString())
                       };
        }

        private void EndConversation(IDbTransaction transaction, Guid conversationHandle)
        {
            ServiceBrokerWrapper.EndConversation(transaction, conversationHandle, _inputQueue, false);
        }

        private Tuple<Guid, TransportMessage> ReceiveFromQueue(IDbTransaction transaction)
        {
            var ssbMessages = ServiceBrokerWrapper.WaitAndReceive(transaction, _inputQueue,
                                                                  SecondsToWaitForMessage.GetValueOrDefault()*1000,
                                                                  ReceiveBatchSize.GetValueOrDefault()).ToArray();
            if (ssbMessages.Length == 0) return null;

            var conversationHandle = ssbMessages.First().ConversationHandle;
            var transportMessage = ExtractTransportMessage(ssbMessages);

            return (transportMessage == null)
                       ? null
                       : Tuple.Create(conversationHandle, transportMessage);
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
            Tuple<Guid, TransportMessage> tuple = null;

            var xman = new ServiceBrokerTransactionManager(ConnectionString);
            xman.RunInTransaction(x => { tuple = ReceiveFromQueue(x); });
            if (tuple == null) return null;

            try
            {
                xman.RunInTransaction(x => EndConversation(x, tuple.Item1));
            }
            catch (Exception e)
            {
                Logger.WarnFormat("Unable to end conversation '{0}'. Reason: '{1}'",
                                  tuple.Item1, e.Message);
            }

            return tuple.Item2;
        }
    }
}