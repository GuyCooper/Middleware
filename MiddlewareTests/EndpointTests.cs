using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;
using Newtonsoft.Json;
using MiddlewareInterfaces;

namespace MiddlewareTests
{
    internal class TestConnection : IConnection
    {
        public List<string> DataList = new List<string>();

        public void CloseConnection()
        {
        }

        public void SendData(byte[] data)
        {
            DataList.Add(MiddlewareUtils.DeserialiseObject<string>(data));
        }
    }

    internal class TestHandler : IMessageHandler
    {
        public List<MiddlewareMessage> Messages = new List<MiddlewareMessage>();
        public void AddHandler(IMessageHandler handler) { }
        public IMessageHandler GetNext() { return null; }
        public bool ProcessMessage(MiddlewareMessage message)
        {
            Messages.Add(message);
            return true;
        }
        public void RemoveEndpoint(string id) { }
    }

    [TestClass]
    public class EndpointTests
    {
        private readonly static string TestChannel = "TestChannel";
        //private readonly static string TestCommand = "SEND_REQUEST";
        private readonly static string TestPayload = "Test Payload";
        private readonly static string TestDestination = "123 456 ";
        private readonly static string TestRequestId = "987 543";


        private Message _CreateTestMessage(string command, string payload)
        {
            return new Message
            {
                Channel = TestChannel,
                Command = command,
                DestinationId = TestDestination,
                Payload = payload,
                Type = MessageType.REQUEST,
                RequestId = TestRequestId
            };
        }

        [TestMethod]
        public void When_creating_empty_endpoint()
        {
            var endpoint = new MiddlewareEndpoint(null, null, null);
            Assert.IsNotNull(endpoint.Id);
        }


        [TestMethod]
        public void When_receiving_null_data()
        {
            var endpoint = new MiddlewareEndpoint(new TestConnection(), new TestHandler(), new DefaultAuthenticationHandler());
            endpoint.DataReceived(null);
            //shouldn't crash
        }

        [TestMethod]
        public void When_receiving_payload__non_authenticated()
        {
            var endpoint = new MiddlewareEndpoint(new TestConnection(), new TestHandler(), new DefaultAuthenticationHandler());
            var payload = MiddlewareUtils.SerialiseObject(_CreateTestMessage(HandlerNames.SENDREQUEST, TestPayload));
            bool bError = false;
            try
            {
                endpoint.DataReceived(payload);
            }
            catch(InvalidOperationException)
            {
                bError = true;
            }
            Assert.IsTrue(bError);
        }

        [TestMethod]
        public void When_authenticating_endpoint()
        {
            var endpoint = new MiddlewareEndpoint(new TestConnection(), new TestHandler(), new DefaultAuthenticationHandler());
            var login = new LoginPayload
            {
                UserName = "admin",
                Password = "password"
            };

            var payload = MiddlewareUtils.SerialiseObject(_CreateTestMessage(HandlerNames.LOGIN, TestPayload));

            endpoint.AuthenticateEndpoint(payload).ContinueWith(t =>
           {
               AuthResponse response = t.Result;
               AuthResult result = response.Result;
               Assert.AreEqual(AuthResult.ResultType.SUCCESS,  result.Result);
               Assert.IsTrue(endpoint.Authenticated);
           });
        }
    }
}
