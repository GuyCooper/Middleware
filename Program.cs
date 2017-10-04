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
            if(args.Length == 0)
            {
                Console.WriteLine(@"syntax: Middleware <port (http://localhost:8080/)> <max connections (10)> <root (//_root = (C:\Projects\Middleware\Middleware)>");
            }

            var url = args.Length > 0 ? args[0] :  "http://localhost:8080/";
            int maxConnections = args.Length > 1 ? int.Parse(args[1]) : 10; //max number of concurrent connections
            string root =  @"C:\Projects\Middleware\Middleware";
            Console.WriteLine("initialising server on port: {0}", url);
            Console.WriteLine("with {0} max concurrent connections", maxConnections);

            IMessageStats stats = new MessageStats("dev", maxConnections);
            IChannel channel = new Channels(stats);
            IHandler publishHandler = new PublishMessageHandler(channel);
            publishHandler.AddHandler(new SendMessageHandler(channel));
            publishHandler.AddHandler(new SendRequestHandler(channel));
            publishHandler.AddHandler(new AddListenerHandler(channel));
            publishHandler.AddHandler(new SubscribeToChannelHandler(channel));
            publishHandler.AddHandler(new RemoveSubscriptionHandler(channel));

            EndpointManager manager = new EndpointManager(publishHandler, stats);

            WSServer server = new WSServer(manager, root);
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
