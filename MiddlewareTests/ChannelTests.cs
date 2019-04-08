using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using MiddlewareInterfaces;

namespace MiddlewareTests
{
    [TestClass]
    public class ChannelTests
    {
        class TestEndpoint : IEndpoint
        {
            private string _id;
            public ManualResetEvent ErrorEvent{ get; private set;}
            public ManualResetEvent SuccessEvent { get; private set; }
            public string ErrorMessage { get; set; }
            public Message ResultMessage { get; set; }
            public int ThreadId { get; private set; }
            public TestEndpoint(string source)
            {
                _id = source ?? "TestId";
                ErrorEvent = new ManualResetEvent(false);
                SuccessEvent = new ManualResetEvent(false);
            }
            public void NotifySessionClosed(string id) { }

            public void DataReceived(byte[] data) { }

            public bool Authenticated { get { return true; } }
            public Task<AuthResponse> AuthenticateEndpoint(byte[] data) { return null; }

            public string DataSent { get; private set; }
            public string Id { get { return _id; } }
            public void SendData(Message message) { DataSent = message.Payload; }
            public void OnError(Message message, string error)
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                ErrorMessage = error;
                ResultMessage = message;
                ErrorEvent.Set();
            }

            public void OnSucess(Message message)
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                ResultMessage = message;
                SuccessEvent.Set();
            }

            public void EndpointClosed() { }
        }

        private MiddlewareMessage _createTestMessage(string channelName, IEndpoint source, string payload = "data")
        {
            return new MiddlewareMessage(
                 new Message
                 {
                     Channel = channelName,
                     Payload = payload,
                     SourceId = source != null ? source.Id : null
                 },
                source
            );
        }

        [TestMethod]
        public void When_adding_async_channel_listener()
        {
            var stats = new MessageStats("test", 10);
            var OuT = new Channels(stats);

            var source = new TestEndpoint(null);
            var message = _createTestMessage("Channel1", source);
            OuT.AddListener(message);
            var result = source.SuccessEvent.WaitOne(5000);
            Assert.IsTrue(result);
            var channelStats = stats.GetChannelsStats().First();
            Assert.AreEqual(1, channelStats.Requests);
            Assert.AreEqual("Channel1", channelStats.ChannelName);
        }

        [TestMethod]
        public void When_sending_message_async_to_null_destination()
        {
            var OuT = new Channels(null);
            var source = new TestEndpoint(null);
            var message = _createTestMessage("Channel1", source);
            OuT.SendMessage(message);
            var result = source.ErrorEvent.WaitOne(30000);
            Assert.IsTrue(result);
            Assert.AreEqual(source.ResultMessage.Channel, "Channel1");
            Assert.IsTrue(source.ErrorMessage.Contains("Invalid Destination"));
        }

        [TestMethod]
        public void When_sending_message_async_to_invalid_destination()
        {
            var UoT = new Channels(null);
            var source = new TestEndpoint(null);
            var message = _createTestMessage("Channel1", source);
            message.Payload.DestinationId = "invalid user";
            UoT.SendMessage(message);
            var result = source.ErrorEvent.WaitOne(5000);
            Assert.IsTrue(result);
            Assert.AreEqual(source.ResultMessage.Channel, "Channel1");
            Assert.IsTrue(source.ErrorMessage.Contains("Invalid Destination"));

        }

        [TestMethod]
        public void When_sending_request_async_to_channel_with_no_listener()
        {
            var UoT = new Channels(null);
            var source = new TestEndpoint(null);
            var message = _createTestMessage("Channel1", source);
            UoT.SendRequest(message);
            var result = source.ErrorEvent.WaitOne(5000);
            Assert.IsTrue(result);
            Assert.AreEqual(source.ResultMessage.Channel, "Channel1");
            Assert.IsTrue(source.ErrorMessage.Contains("No Listener"));
        }

        [TestMethod]
        public void When_sending_request_async_to_channel_with_listener()
        {
            var stats = new MessageStats("test", 10);
            var UoT = new Channels(stats);
            var source1 = new TestEndpoint(null);
            var source2 = new TestEndpoint(null);
            var message1 = _createTestMessage("Channel1", source1);
            var message2 = _createTestMessage("Channel1", source2, "test");
            UoT.AddListener(message1);
            var result = source1.SuccessEvent.WaitOne(30000);
            Assert.IsTrue(result);
            UoT.SendRequest(message2);
            result = source2.SuccessEvent.WaitOne(30000);
            Assert.IsTrue(result);
            Assert.AreEqual(source1.DataSent, "test");
            var channelStats = stats.GetChannelsStats().First();
            Assert.AreEqual(2, channelStats.Requests);
            Assert.AreEqual("Channel1", channelStats.ChannelName);

        }

        [TestMethod]
        public void When_adding_multiple_async_channel_listener()
        {
            var stats = new MessageStats("test", 10);
            var OuT = new Channels(stats);

            var source1 = new TestEndpoint(null);
            var source2 = new TestEndpoint(null);
            var source3 = new TestEndpoint(null); 
            var source4 = new TestEndpoint(null);
            var message1 = _createTestMessage("Channel1", source1);
            var message2 = _createTestMessage("Channel2", source2);
            var message3 = _createTestMessage("Channel3", source3);
            var message4 = _createTestMessage("Channel4", source4);
            OuT.AddListener(message1);
            OuT.AddListener(message2);
            OuT.AddListener(message3);
            OuT.AddListener(message4);
            var result = source1.SuccessEvent.WaitOne(30000) &&
                        source2.SuccessEvent.WaitOne(30000) &&
                        source3.SuccessEvent.WaitOne(30000) &&
                        source4.SuccessEvent.WaitOne(30000);
            Assert.IsTrue(result);
            Assert.IsTrue(source1.ThreadId > 0);
            Assert.AreEqual(source1.ThreadId, source2.ThreadId);
            Assert.AreEqual(source2.ThreadId, source3.ThreadId);
            Assert.AreEqual(source3.ThreadId, source4.ThreadId);

            var channelStats = stats.GetChannelsStats().ToList();
            Assert.AreEqual(4, channelStats.Count);
        }

        [TestMethod]
        public void When_requesting_without_a_listener()
        {
            var UoT = new Channel();
            bool caught = false;
            try
            {
                var source = new TestEndpoint(null);
                UoT.SendRequest(_createTestMessage("channel1", source));
            }
            catch(MissingListenerException)
            {
                caught = true;
            }
            Assert.IsTrue(caught);
        }

        [TestMethod]
        public void When_requesting_with_a_listener_and_invalid_source()
        {
            var source1 = new TestEndpoint(null);
            var testMessage1 = _createTestMessage("channel1", source1);
            var testMessage2 = _createTestMessage("channel1", null, "test");
            var OuT = new Channel();
            var caught = false;
            OuT.AddListener(testMessage1);
            try
            {
                OuT.SendRequest(testMessage2);
            }
            catch(InvalidSourceException)
            {
                caught = true;
            }
            Assert.IsTrue(caught);
            //Assert.AreEqual(source1.DataSent, "test");
        }

        [TestMethod]
        public void When_requesting_with_a_listener()
        {
            var source1 = new TestEndpoint(null);
            var source2 = new TestEndpoint(null);
            var testMessage1 = _createTestMessage("channel1", source1);
            var testMessage2 = _createTestMessage("channel1", source2, "test");
            var OuT = new Channel();
            OuT.AddListener(testMessage1);
            OuT.SendRequest(testMessage2);
            Assert.AreEqual( source1.DataSent, "test");
        }

        [TestMethod]
        public void When_sending_message_to_invalid_destination()
        {
            bool caught = false;
            var OuT = new Channel();
            try
            {
                var source1 = new TestEndpoint(null);
                OuT.SendMessage(_createTestMessage("channel1", source1));
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
            var OuT = new Channel();
            var source1 = new TestEndpoint("Test1");
            var source2 = new TestEndpoint("Test2");
            var message1 = _createTestMessage("channel1", source1);
            var message2 = _createTestMessage("channel1", source2, "test");
            message2.Payload.DestinationId = source1.Id;
            OuT.AddListener(message2);
            OuT.AddSubscriber(message1);
            OuT.SendRequest(message1);
            OuT.SendMessage(message2);
            Assert.AreEqual(source1.DataSent, "test");
        }

        [TestMethod]
        public void When_broadcasting_message_to_multiple_destinations()
        {
            var OuT = new Channel();
            var source1 = new TestEndpoint("1");
            var source2 = new TestEndpoint("2");
            var source3 = new TestEndpoint("3");
            var message1 = _createTestMessage("channel1", source1);
            var message2 = _createTestMessage("channel1", source2);
            var message3 = _createTestMessage("channel1", source3, "test");
            OuT.AddSubscriber(message1);
            OuT.AddSubscriber(message2);
            OuT.PublishMessage(message3);
            Assert.AreEqual(source1.DataSent, "test");
            Assert.AreEqual(source2.DataSent, "test");
        }

        private FullStats DeserialisedResults(string results)
        {
            using (var ss = new StringReader(results))
            {
                XmlSerializer serialiser = new XmlSerializer(typeof(FullStats));
                return (FullStats)serialiser.Deserialize(ss);
            }
        }

        [TestMethod]
        public void TestChannelStatsRequest()
        {
            var UoT = new MessageStats("test", 10);
            var source = new TestEndpoint(null);
            var message = _createTestMessage("Channel1", source);
            message.Payload.Type = MessageType.REQUEST;
            UoT.UpdateChannelStats(message.Payload);
            var result = DeserialisedResults(UoT.ToXML());
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Channels.Length);
            Assert.AreEqual(result.Channels[0].ChannelName,"Channel1");
            Assert.AreEqual(1, result.Channels[0].Requests);
        }

        [TestMethod]
        public void TestChannelStatsUpdate()
        {
            var UoT = new MessageStats("test", 10);
            var source = new TestEndpoint(null);
            var message = _createTestMessage("Channel1", source);
            message.Payload.Type = MessageType.UPDATE;
            UoT.UpdateChannelStats(message.Payload);
            var result = DeserialisedResults(UoT.ToXML());
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Channels.Length);
            Assert.AreEqual(result.Channels[0].ChannelName, "Channel1");
            Assert.AreEqual(4, result.Channels[0].DataUpdates);
        }

        [TestMethod]
        public void TestStatsCreation()
        {
            var UoT = new MessageStats("test", 10);
            var result = DeserialisedResults(UoT.ToXML());
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result.LicensedTo);
            Assert.AreEqual(10, result.MaxConnections);
        }

        [TestMethod]
        public void TestStatsOpenConnection()
        {
            var UoT = new MessageStats("test", 10);
            UoT.NewConnection("123", "abcd", "test", "1.0", false);
            UoT.NewConnection("456", "abcd", "test", "1.1", false);
            var result = DeserialisedResults(UoT.ToXML());
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Connections.Length);
            Assert.AreEqual(2, result.CurrentConnections);
        }

        [TestMethod]
        public void TestStatsCloseConnection()
        {
            var UoT = new MessageStats("test", 10);
            UoT.NewConnection("123", "abcd", "test", "1.0", false);
            UoT.NewConnection("456", "abcd", "test", "1.0", false);
            UoT.CloseConnection("123", false);
            var result = DeserialisedResults(UoT.ToXML());
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Connections.Length);
            Assert.AreEqual(1, result.CurrentConnections);
            Assert.AreEqual("456", result.Connections[0].Id);
        }

        [TestMethod]
        public void TestStatsConnectionUpdate()
        {
            var UoT = new MessageStats("test", 10);
            var source = new TestEndpoint("123");
            UoT.NewConnection(source.Id, "abcd", "test", "1.0", true);
            var message = _createTestMessage("Channel1", source);
            message.Payload.Type = MessageType.UPDATE;
            UoT.UpdateChannelStats(message.Payload);
            var result = DeserialisedResults(UoT.ToXML());
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Connections.Length);
            Assert.AreEqual(4, result.Connections[0].DataUpdates);
        }
    }
}
