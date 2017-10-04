using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;
using Newtonsoft.Json;

namespace MiddlewareTests
{
    [TestClass]
    public class MessageTests
    {
        //[JsonObject(MemberSerialization.OptIn)]
        class TestMessage
        {
            public string Command { get; set; }
        }

        [TestMethod]
        public void When_deserialising_a_Message()
        {
            //var data = "{ \"Command\" :\"ADDLISTENER\"}";
            //TestMessage message = JsonConvert.DeserializeObject<TestMessage>(data);

            var data = "{\"RequestId\":\"test_1\",\"Command\":\"ADDLISTENER\",\"Channel\":\"test1\",\"MessageType\":0,\"Payload\":null,\"DestinationId\":null, \"SourceId\":null}";
            Message message = JsonConvert.DeserializeObject<Message>(data);
            Assert.IsNotNull(message);
            Assert.IsNotNull(message.Channel);
            Assert.IsNotNull(message.Command);
            Assert.IsNotNull(message.RequestId);
        }
    }
}
