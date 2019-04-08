using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;
using MiddlewareInterfaces;

namespace MiddlewareTests
{
    class TestChannel : IChannel
    {
        public MiddlewareMessage ListenerMessageResult { get; private set; }
        public MiddlewareMessage SubscriberMessageResult { get; private set; }
        public MiddlewareMessage PublishMessageResult { get; private set; }
        public MiddlewareMessage RemoveSubscriberResult { get; private set; }
        public MiddlewareMessage SendMessageResult { get; private set; }
        public MiddlewareMessage SendRequestResult { get; private set; }

        public void AddListener(MiddlewareMessage message)
        {
            ListenerMessageResult = message;
        }

        public void AddSubscriber(MiddlewareMessage message)
        {
            SubscriberMessageResult = message;
        }

        public void PublishMessage(MiddlewareMessage message)
        {
            PublishMessageResult = message;
        }

        public void RemoveSubscriber(MiddlewareMessage message)
        {
            RemoveSubscriberResult = message;
        }

        public void SendMessage(MiddlewareMessage message)
        {
            SendMessageResult = message;
        }

        public void SendRequest(MiddlewareMessage message)
        {
            SendRequestResult = message;
        }

        public void RemoveEndpoint(string id)
        {

        }
    }

    [TestClass]
    public class HandlerTests
    {
        private TestChannel _testChannel;
        private IMessageHandler _testhandler;

        [TestInitialize]
        public void Setup()
        {
            _testChannel = new TestChannel();
            _testhandler = new SubscribeToChannelHandler(_testChannel);
            _testhandler.AddHandler(new RemoveSubscriptionHandler(_testChannel));
            _testhandler.AddHandler(new SendMessageHandler(_testChannel));
            _testhandler.AddHandler(new AddListenerHandler(_testChannel));
            _testhandler.AddHandler(new SendRequestHandler(_testChannel ));
            _testhandler.AddHandler(new PublishMessageHandler(_testChannel));
        }

        private MiddlewareMessage _CreateTestMessage(string command)
        {
            return new MiddlewareMessage(
                new Message
                {
                    Channel = "TEST",
                    Command = command,
                    Payload = "some data"
                },
                null
            );
        }
        [TestMethod]
        public void When_adding_a_listener()
        {
            var message = _CreateTestMessage(HandlerNames.ADDLISTENER);
            var result = _testhandler.ProcessMessage(message);
            Assert.IsTrue(result);
            Assert.AreEqual(_testChannel.ListenerMessageResult, message);
        }

        [TestMethod]
        public void When_adding_a_publisher()
        {
            var message = _CreateTestMessage(HandlerNames.PUBLISHMESSAGE);
            var result = _testhandler.ProcessMessage(message);
            Assert.IsTrue(result);
            Assert.AreEqual(_testChannel.PublishMessageResult, message);
        }

        [TestMethod]
        public void When_adding_a_subscriber()
        {
            var message = _CreateTestMessage(HandlerNames.SUBSCRIBETOCHANNEL);
            var result = _testhandler.ProcessMessage(message);
            Assert.IsTrue(result);
            Assert.AreEqual(_testChannel.SubscriberMessageResult, message);
        }

        [TestMethod]
        public void When_adding_a_sendrequest()
        {
            var message = _CreateTestMessage(HandlerNames.SENDREQUEST);
            var result = _testhandler.ProcessMessage(message);
            Assert.IsTrue(result);
            Assert.AreEqual(_testChannel.SendRequestResult, message);
        }

        [TestMethod]
        public void When_adding_a_remove_subscriber()
        {
            var message = _CreateTestMessage(HandlerNames.REMOVESUBSCRIPTION);
            var result = _testhandler.ProcessMessage(message);
            Assert.IsTrue(result);
            Assert.AreEqual(_testChannel.RemoveSubscriberResult, message);
        }

        [TestMethod]
        public void When_adding_a_sendmessage()
        {
            var message = _CreateTestMessage(HandlerNames.SENDMESSAGE);
            var result = _testhandler.ProcessMessage(message);
            Assert.IsTrue(result);
            Assert.AreEqual(_testChannel.SendMessageResult, message);
        }

        [TestMethod]
        public void When_adding_an_invalidcommand()
        {
            var message = _CreateTestMessage("INVALID");
            var result = _testhandler.ProcessMessage(message);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void When_adding_a_null_message()
        {
            var result = _testhandler.ProcessMessage(null);
            Assert.IsFalse(result);
        }
    }
}
