using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace Middleware
{
    class Program
    {
        static AutoResetEvent _shutdownEvent = new AutoResetEvent(false);
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine(@"syntax: Middleware <port (8080)> <max connections (10)> <root (//_root = (C:\Projects\Middleware\Middleware)>");
            }

            //var url = string.Format("https://localhost:{0}/", args.Length > 0 ? args[0] :  "8080");
            //string[] urls = { "http://*:8080", "https://*:8443"};
            string[] urls = { "http://localhost:8080/" };
            int maxConnections = args.Length > 1 ? int.Parse(args[1]) : 10; //max number of concurrent connections
            string root =  @"C:\Projects\Middleware\Middleware";
            Console.WriteLine("initialising server on port: {0}", urls);
            Console.WriteLine("with {0} max concurrent connections", maxConnections);

            IMessageStats stats = new MessageStats("dev", maxConnections);
            IChannel channel = new Channels(stats);
            IHandler publishHandler = new PublishMessageHandler(channel);
            IAuthenitcationHandler authHandler = new DefaultAuthenticationHandler();
            publishHandler.AddHandler(new SendMessageHandler(channel));
            publishHandler.AddHandler(new SendRequestHandler(channel));
            publishHandler.AddHandler(new AddListenerHandler(channel));
            publishHandler.AddHandler(new SubscribeToChannelHandler(channel));
            publishHandler.AddHandler(new RemoveSubscriptionHandler(channel));

            EndpointManager manager = new EndpointManager(publishHandler, authHandler, stats);

            WSServer server = new Endpointserver(manager, root, stats);
            server.Start(urls, maxConnections);

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
