using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace Middleware
{
    class MiddlewareConfig
    {
        public MiddlewareConfig(string[] args)
        {
            ConfigParser parser = new ConfigParser();
            parser.AddParameter("H", "server url","http://localhost:8080/", (val) => { this.URLS = val; });
            parser.AddParameter("S", "auth url", "http://localhost:9092/", (val) => { this.AUTHURLS = val; });
            parser.AddParameter("M", "max client connections", "10", (val) => { this.MaxConnections = int.Parse(val); });
            parser.AddParameter("N", "max auth client connections", "1", (val) => { this.MaxAuthConnections = int.Parse(val); });
            parser.AddParameter("R", "root folder", @"C:\Projects\Middleware\Middleware", (val) => { this.RootFolder = val; });
            parser.AddParameter("T", "authg response timeout", "30000", (val) => { this.AuthTimeout = int.Parse(val); });

            Console.WriteLine("parsing command line arguments...");
            parser.ParseCommandLine(args);
            parser.LogValues();
        }

        public string URLS { get; private set; }
        public string AUTHURLS { get; private set; }
        public int MaxConnections { get; private set; }
        public int MaxAuthConnections { get; private set; }
        public string RootFolder { get; private set; }
        public int AuthTimeout { get; private set; }
    }

    class Program
    {
        static AutoResetEvent _shutdownEvent = new AutoResetEvent(false);
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine(@"syntax: Middleware <port (8080)> <max connections (10)> <root (//_root = (C:\Projects\Middleware\Middleware)>");
            }

            var config = new MiddlewareConfig(args);

            IMessageStats stats = new MessageStats("dev", config.MaxConnections);
            IAuthenticationHandler authHandler = new DefaultAuthenticationHandler();

            WSServer authServer = InitialiseAuthenticationServer(config, authHandler, stats);
            WSServer server = InitialiseEndpointServer(config, authHandler, stats);

            Console.WriteLine("server initialised");

            _shutdownEvent.WaitOne();

            Console.WriteLine("shutting down server");
            authServer.Stop();
            server.Stop();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _shutdownEvent.Set();
            };
        }

        private static WSServer InitialiseEndpointServer(MiddlewareConfig config, 
                                                         IAuthenticationHandler authHandler,
                                                         IMessageStats stats)
        {
            IChannel channel = new Channels(stats);
            IMessageHandler messageHandler = new PublishMessageHandler(channel);
            messageHandler.AddHandler(new SendMessageHandler(channel));
            messageHandler.AddHandler(new SendRequestHandler(channel));
            messageHandler.AddHandler(new AddListenerHandler(channel));
            messageHandler.AddHandler(new SubscribeToChannelHandler(channel));
            messageHandler.AddHandler(new RemoveSubscriptionHandler(channel));

            Console.WriteLine("initialising endpoint server...");
            EndpointManager manager = new EndpointManager(messageHandler, authHandler, stats);

            WSServer server = new Endpointserver(manager, config.RootFolder, stats);
            Task.Factory.StartNew(() =>
            {
                server.Start(config.URLS.Split(','), config.MaxConnections);
            });
            return server;
        }

        private static WSServer InitialiseAuthenticationServer(MiddlewareConfig config,
                                                         IAuthenticationHandler rootAuthHandler,
                                                         IMessageStats stats)
        {
            AuthRequestCache authCache = new AuthRequestCache(config.AuthTimeout);
            var messageHandler = new AuthLoginResponseHandler(authCache);
            messageHandler.AddHandler(new AuthRegisterMessageHandler(rootAuthHandler, authCache));

            Console.WriteLine("initialising auth endpoint server...");
            EndpointManager manager = new AuthenticationManager(messageHandler, rootAuthHandler, stats);

            WSServer server = new WSServer(manager);
            Task.Factory.StartNew(() =>
            {
                server.Start(config.AUTHURLS.Split(','), config.MaxAuthConnections);
            });
            return server;
        }
    }
}
