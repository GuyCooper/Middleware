using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;
using Moq;

namespace MiddlewareTests
{
    class TestChannel : IChannel
    {
        public Message ListenerMessageResult { get; private set; }
        public Message SubscriberMessageResult { get; private set; }
        public Message PublishMessageResult { get; private set; }
        public Message RemoveSubscriberResult { get; private set; }
        public Message SendMessageResult { get; private set; }
        public Message SendRequestResult { get; private set; }

        public void AddListener(Message message)
        {
            ListenerMessageResult = message;
        }

        public void AddSubscriber(Message message)
        {
            SubscriberMessageResult = message;
        }

        public void PublishMessage(Message message)
        {
            PublishMessageResult = message;
        }

        public void RemoveSubscriber(Message message)
        {
            RemoveSubscriberResult = message;
        }

        public void SendMessage(Message message)
        {
            SendMessageResult = message;
        }

        public void SendRequest(Message message)
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
        private IHandler _testhandler;

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

        private Message _CreateTestMessage(string command)
        {
            return new Message
            {
                Channel = "TEST",
                Command = command,
                Data = "some data"
            };
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
            bool exp = false;
            try
            {
                var result = _testhandler.ProcessMessage(null);
            }
            catch(ArgumentException)
            {
                exp = true;
            }
            Assert.IsTrue(exp);
        }
    }
}
