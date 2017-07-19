using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;
using System.Threading;
namespace MiddlewareTests
{
    [TestClass]
    public class ChannelTests
    {
        class TestEndpoint : IEndpoint
        {
            private string _id;
            public TestEndpoint(string source)
            {
                _id = source ?? "TestId";
            }
            public string DataSent { get; private set; }
            public string Id { get { return _id; } }
            public void SendData(Message message) { DataSent = message.Data; }
        }

        private Message _createTestMessage(string channelName, string payload = "data", string sourceid = null)
        {
            return new Message
            {
                Channel = channelName,
                Data = payload,
                Source = new TestEndpoint(sourceid)
            };
        }

        [TestMethod]
        public void When_adding_async_channel_listener()
        {
            var complete = new ManualResetEvent(false);
            var UoT = new Channels((channel) =>
           {
               Assert.AreEqual(channel, "Channel1");
               complete.Set();
           });

            var message = _createTestMessage("Channel1");
            UoT.AddListener(message);
            var result = complete.WaitOne(5000);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void When_adding_multiple_async_channel_listener()
        {
            var complete = new ManualResetEvent(false);
            var threadid = 0;
            var UoT = new Channels((channel) =>
            {
                //this callback can only be fired one one thread
                complete.Set();
                if(threadid == 0)
                {
                    threadid = Thread.CurrentThread.ManagedThreadId;
                }
                else
                {
                    Assert.AreEqual(threadid, Thread.CurrentThread.ManagedThreadId);
                }
                
            });

            var message = _createTestMessage("Channel1");
            var message1 = _createTestMessage("Channel2");
            var message2 = _createTestMessage("Channel3");
            var message3 = _createTestMessage("Channel4");
            UoT.AddListener(message);
            UoT.AddListener(message1);
            UoT.AddListener(message2);
            UoT.AddListener(message3);
            var result = complete.WaitOne(5000);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void When_requesting_without_a_listener()
        {
            var UoT = new Channel();
            bool caught = false;
            try
            {
                UoT.SendRequest(_createTestMessage("channel1"));
            }
            catch(MissingListenerException)
            {
                caught = true;
            }
            Assert.IsTrue(caught);
        }

        [TestMethod]
        public void When_requesting_with_a_listener()
        {
            var testMessage1 = _createTestMessage("channel1");
            var testMessage2 = _createTestMessage("channel1", "test");
            var UoT = new Channel();
            UoT.AddListener(testMessage1);
            UoT.SendRequest(testMessage2);

            var endpoint1 = testMessage1.Source as TestEndpoint;
            Assert.IsNotNull(endpoint1);
            Assert.AreEqual(endpoint1.DataSent, "test");
        }

        [TestMethod]
        public void When_sending_message_to_invalid_destination()
        {
            bool caught = false;
            var UoT = new Channel();
            try
            {
                UoT.SendMessage(_createTestMessage("channel1"));
            }
            catch(InvalidDestinationException)
            {
                caught = true;
            }
            Assert.IsTrue(caught);
        }

        [TestMethod]
        public void When_sending_message_to_valid_destination()
        {
            var UoT = new Channel();
            var message1 = _createTestMessage("channel1");
            var message2 = _createTestMessage("channel1", "test");
            message2.DestinationId = message1.Source.Id;
            UoT.AddSubscriber(message1);
            UoT.SendMessage(message2);
            var endpoint = message1.Source as TestEndpoint;
            Assert.IsNotNull(endpoint);
            Assert.AreEqual(endpoint.DataSent, "test");
        }

        [TestMethod]
        public void When_broadcasting_message_to_multiple_destinations()
        {
            var UoT = new Channel();
            var message1 = _createTestMessage("channel1",null,"1");
            var message2 = _createTestMessage("channel1", null, "2");
            var message3 = _createTestMessage("channel1", "test");
            UoT.AddSubscriber(message1);
            UoT.AddSubscriber(message2);
            UoT.PublishMessage(message3);
            var endpoint1 = message1.Source as TestEndpoint;
            var endpoint2 = message2.Source as TestEndpoint;
            Assert.IsNotNull(endpoint1);
            Assert.IsNotNull(endpoint2);
            Assert.AreEqual(endpoint1.DataSent, "test");
            Assert.AreEqual(endpoint2.DataSent, "test");
        }

    }
}
