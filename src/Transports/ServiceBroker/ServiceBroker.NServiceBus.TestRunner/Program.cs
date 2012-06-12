using System;
using System.Threading;
using NServiceBus;

namespace TestRunner
{
    internal class Program
    {
        private static void Main()
        {
            System.Transactions.TransactionManager.DistributedTransactionStarted +=
                TransactionManagerOnDistributedTransactionStarted;

            var bus = Configure.With()
                .Log4Net()
                .StructureMapBuilder()
                .XmlSerializer()
                .UnicastBus()
                .DoNotAutoSubscribe()
                .LoadMessageHandlers()
                .ServiceBrokerTransport()
                .ConnectionString(@"Server=.\SQLEXPRESS;Database=ServiceBroker_HelloWorld;Trusted_Connection=True;")
                .ErrorService("ErrorService")
                .CreateBus()
                .Start();

            bus.Send("ServiceA", new TestMessage
                                     {
                                         Content = "Hello World - Send()",
                                     }).Register<TestMessage>(Console.WriteLine);

            bus.SendLocal(new TestMessage
                              {
                                  Content = "Hello World - SendLocal()",
                              });

            while (true)
            {
                Thread.Sleep(100);
            }
        }

        private static void TransactionManagerOnDistributedTransactionStarted(object sender,
                                                                             System.Transactions.TransactionEventArgs e)
        {
            Console.WriteLine("Distributed Transaction Started");
        }
    }

    [Serializable]
    public class TestMessage : IMessage
    {
        public string Content { get; set; }
    }

    public class TestMessageHandler : IMessageHandler<TestMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(TestMessage message)
        {
            Bus.Return(42);
            throw new Exception("Testing Exception Management");
        }
    }
}