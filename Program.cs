using System;
using System.Threading.Tasks;
using System.Threading;
using NLog;

namespace Middleware
{
    /// <summary>
    /// MiddlewareConfig class. Parses the command line parameters and stores the values in 
    /// its piblic properties.
    /// </summary>
    class MiddlewareConfig
    {
        #region Constructor

        /// <summary>
        /// Constructor. Parse the command line parameters
        /// </summary>
        public MiddlewareConfig(string[] args)
        {
            ConfigParser parser = new ConfigParser();
            parser.AddParameter("H", "server url", "http://localhost:8080/MWARE/", (val) => { this.URLS = val; });
            //parser.AddParameter("H", "server url","https://localhost:8443/", (val) => { this.URLS = val; });
            parser.AddParameter("S", "auth url", "http://localhost:9092/", (val) => { this.AUTHURLS = val; });
            parser.AddParameter("M", "max client connections", "10", (val) => { this.MaxConnections = int.Parse(val); });
            parser.AddParameter("N", "max auth client connections", "1", (val) => { this.MaxAuthConnections = int.Parse(val); });
            parser.AddParameter("R", "root folder", @"C:\Projects\Middleware\Middleware", (val) => { this.RootFolder = val; });
            parser.AddParameter("T", "authg response timeout", "30000", (val) => { this.AuthTimeout = int.Parse(val); });

            parser.ParseCommandLine(args);
            parser.LogValues();
        }
        #endregion

        #region Public Properties

        public string URLS { get; private set; }
        public string AUTHURLS { get; private set; }
        public int MaxConnections { get; private set; }
        public int MaxAuthConnections { get; private set; }
        public string RootFolder { get; private set; }
        public int AuthTimeout { get; private set; }

        #endregion
    }

    /// <summary>
    /// Main Static Program class.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main Entry point into process.
        /// </summary>
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                logger.Log(LogLevel.Info, @"syntax: Middleware <port (8080)> <max connections (10)> <root (//_root = (C:\Projects\Middleware\Middleware)>");
            }

            var config = new MiddlewareConfig(args);

            IMessageStats stats = new MessageStats("dev", config.MaxConnections);
            IAuthenticationHandler authHandler = new DefaultAuthenticationHandler();

            WSServer authServer = InitialiseAuthenticationServer(config, authHandler, stats);
            WSServer server = InitialiseEndpointServer(config, authHandler, stats);

            logger.Log(LogLevel.Info, "server initialised");

            _shutdownEvent.WaitOne();

            logger.Log(LogLevel.Info, "shutting down server");

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

            logger.Log(LogLevel.Info, "initialising endpoint server...");
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

            logger.Log(LogLevel.Info, "initialising auth endpoint server...");
            EndpointManager manager = new AuthenticationManager(messageHandler, rootAuthHandler, stats);

            WSServer server = new WSServer(manager);
            Task.Factory.StartNew(() =>
            {
                server.Start(config.AUTHURLS.Split(','), config.MaxAuthConnections);
            });
            return server;
        }

        #region Private Static Data Members

        static AutoResetEvent _shutdownEvent = new AutoResetEvent(false);

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
