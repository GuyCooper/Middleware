using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace Middleware
{
    enum MessageType
    {
        REQUEST = 0,
        UPDATE = 1,
        RESPONSE_ERROR = 2,
        RESPONSE_SUCCESS = 3
    }

    class Location
    {
        string Name { get; set; }
    }

    interface Payload
    {
        string SerialisePayload();
    }

    //[JsonObject(MemberSerialization.OptIn)]
    class Message
    {
        public MessageType Type { get; set; }
        public string RequestId { get; set; }
        public string Command { get; set; }
        public string Channel { get; set; }
        [JsonIgnore]
        public IEndpoint Source { get; set; } //internal use only
        public string SourceId { get; set; }
        public string DestinationId { get; set; }
        public string Payload { get; set; }
    }


    class Program
    {
        static AutoResetEvent _shutdownEvent = new AutoResetEvent(false);
        static void Main(string[] args)
        {
            var url = "http://localhost:8080/";
            int maxConnections = 3; //max number of concurrent connections
            Console.WriteLine("initialising server on port: {0}", url);

            IChannel channel = new Channels();
            IHandler publishHandler = new PublishMessageHandler(channel);
            publishHandler.AddHandler(new SendMessageHandler(channel));
            publishHandler.AddHandler(new SendRequestHandler(channel));
            publishHandler.AddHandler(new AddListenerHandler(channel));
            publishHandler.AddHandler(new SubscribeToChannelHandler(channel));
            publishHandler.AddHandler(new RemoveSubscriptionHandler(channel));

            EndpointManager manager = new EndpointManager(publishHandler);

            WSServer server = new WSServer(manager);
            server.Start(url, maxConnections);

            Console.WriteLine("server initialised");

            _shutdownEvent.WaitOne();

            Console.WriteLine("shutting down server");
            server.Stop();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _shutdownEvent.Set();
            };
            //websocket manager
            //messaging (p2p)
            //subscriptions (broadcast)
            //System.Net.WebSockets;
            //initalise TCP server
            //initialise Websocket server
            //wait until shutdown

        }

        private static void Console_CancelKeyPress()
        {
            throw new NotImplementedException();
        }
    }
}
