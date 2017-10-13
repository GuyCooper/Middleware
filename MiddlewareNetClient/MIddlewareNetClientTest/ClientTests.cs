using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiddlewareNetClient;
using Newtonsoft.Json;
using Middleware;

namespace MIddlewareNetClientTest
{
    internal class TestSession : ISession
    {

        public string Message { get; private set; }

        public TestSession()
        {
        }

        public void SendMessage(string message)
        {
            Message = message;
        }

        public void StartDispatcher()
        {
        }
    }

    [TestClass]
    public class ClientTests
    {
        private readonly static string TestChannel = "TestChannel";
        private readonly static string TestPayload = "Test Payload";
        private readonly static string TestDestination = "123 456 ";
        private readonly static string TestRequestId = "987 543";

        private readonly static Message TestSendMessage = new Message
        {
            Channel = TestChannel,
            Command = "SENDREQUEST",
            DestinationId = TestDestination,
            Payload = TestPayload,
            Type = MessageType.REQUEST,
            RequestId = TestRequestId
        };

        private readonly static Message TestPublishMessage = new Message
        {
            Channel = TestChannel,
            Command = "PUBLISHMESSAGE",
            DestinationId = TestDestination,
            Payload = TestPayload,
            Type = MessageType.UPDATE,
            RequestId = TestRequestId
        };

        [TestMethod]
        public void When_instantiating_Manager()
        {
            var manager = new MiddlewareManager();
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public void when_subscribing_to_a_channel()
        {
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, null, null);
            bool success = manager.SubscribeToChannel(session, prms);
            Assert.IsTrue(success);

            var result = JsonConvert.DeserializeObject<Message>(session.Message);
            Assert.AreEqual(result.Channel, TestChannel);
            Assert.AreEqual(result.Command, "SUBSCRIBETOCHANNEL");
        }

        [TestMethod]
        public void when_subscribing_to_a_channel_with_success()
        {
            var callbackresult = false;
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, (s, d) => { callbackresult = true; }, (s, d) => { callbackresult = false; });
            bool success = manager.SubscribeToChannel(session, prms);
            Assert.IsTrue(success);

            var result = JsonConvert.DeserializeObject<Message>(session.Message);
            result.Type = MessageType.RESPONSE_SUCCESS;
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(result));
            Assert.IsTrue(callbackresult);
        }

        [TestMethod]
        public void when_subscribing_to_a_channel_with_failure()
        {
            var callbackresult = false;
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, (s, d) => { callbackresult = false; }, (s, d) => { callbackresult = true; });
            bool success = manager.SubscribeToChannel(session, prms);
            Assert.IsTrue(success);

            var result = JsonConvert.DeserializeObject<Message>(session.Message);
            result.Type = MessageType.RESPONSE_ERROR;
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(result));
            Assert.IsTrue(callbackresult);
        }

        [TestMethod]
        public void when_Adding_listener_to_a_channel_with_success()
        {
            var callbackresult = false;
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, (s, d) => { callbackresult = true; }, (s, d) => { callbackresult = false; });
            bool success = manager.AddChannelListener(session, prms);
            Assert.IsTrue(success);

            var result = JsonConvert.DeserializeObject<Message>(session.Message);
            Assert.AreEqual("ADDLISTENER", result.Command);
            result.Type = MessageType.RESPONSE_SUCCESS;
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(result));
            Assert.IsTrue(callbackresult);
        }


        [TestMethod]
        public void when_Adding_listener_to_a_channel_with_failure()
        {
            var callbackresult = false;
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, (s, d) => { callbackresult = false; }, (s, d) => { callbackresult = true; });
            bool success = manager.AddChannelListener(session, prms);
            Assert.IsTrue(success);

            var result = JsonConvert.DeserializeObject<Message>(session.Message);
            Assert.AreEqual("ADDLISTENER", result.Command);
            result.Type = MessageType.RESPONSE_ERROR;
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(result));
            Assert.IsTrue(callbackresult);
        }


        [TestMethod]
        public void when_sending_message_to_a_channel_with_success()
        {
            var callbackresult = false;
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, (s, d) => { callbackresult = true; }, (s, d) => { callbackresult = false; });
            bool success = manager.SendMessageToChannel(session, prms, TestPayload, TestDestination);
            Assert.IsTrue(success);

            var result = JsonConvert.DeserializeObject<Message>(session.Message);
            Assert.AreEqual("SENDMESSAGE", result.Command);
            Assert.AreEqual(TestPayload, result.Payload);
            Assert.AreEqual(TestDestination, result.DestinationId);
            result.Type = MessageType.RESPONSE_SUCCESS;
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(result));
            Assert.IsTrue(callbackresult);
        }

        [TestMethod]
        public void when_sending_message_to_a_channel_with_failure()
        {
            var callbackresult = false;
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, (s, d) => { callbackresult = false; }, (s, d) => { callbackresult = true; });
            bool success = manager.SendMessageToChannel(session, prms, TestPayload, TestDestination);
            Assert.IsTrue(success);

            var result = JsonConvert.DeserializeObject<Message>(session.Message);
            Assert.AreEqual("SENDMESSAGE", result.Command);
            Assert.AreEqual(TestPayload, result.Payload);
            Assert.AreEqual(TestDestination, result.DestinationId);
            result.Type = MessageType.RESPONSE_ERROR;
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(result));
            Assert.IsTrue(callbackresult);
        }

        [TestMethod]
        public void when_sending_message_to_a_channel_with_no_destination()
        {
            var session = new TestSession();
            var manager = new MiddlewareManager();
            var prms = new MiddlewareRequestParams(TestChannel, null, null);
            bool error = false;
            try
            {
                bool success = manager.SendMessageToChannel(session, prms, TestPayload, null);
            }
            catch (ArgumentException)
            {
                error = true;
            }
            Assert.IsTrue(error);
        }

        [TestMethod]
        public void when_receiving_a_send_request_message()
        {
            var session = new TestSession();
            var manager = new MiddlewareManager();
            manager.RegisterMessageCallbackFunction((s, m) => {
                Assert.AreEqual("SENDREQUEST", m.Command);
                Assert.AreEqual(TestRequestId, m.RequestId);
                Assert.AreEqual(TestPayload, m.Payload);
            });

            var prms = new MiddlewareRequestParams(TestChannel, null, null);
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(TestSendMessage));
        }

        [TestMethod]
        public void when_receiving_a_publish_update_message()
        {
            var session = new TestSession();
            var manager = new MiddlewareManager();
            manager.RegisterMessageCallbackFunction((s, m) => {
                Assert.AreEqual("PUBLISHMESSAGE", m.Command);
                Assert.AreEqual(TestRequestId, m.RequestId);
                Assert.AreEqual(TestPayload, m.Payload);
            });

            var prms = new MiddlewareRequestParams(TestChannel, null, null);
            manager.OnMessageCallback(session, JsonConvert.SerializeObject(TestPublishMessage));
        }
    }
}
