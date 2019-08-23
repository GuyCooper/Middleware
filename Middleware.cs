using System;
using System.Threading;
using NLog;

namespace Middleware
{
    /// <summary>
    /// Main Static Program class.
    /// </summary>
    class Middleware
    {
        #region Main Entry Point

        /// <summary>
        /// Main Entry point into process.
        /// </summary>
        static void Main(string[] args)
        {
            var configFile = "MiddlewareConfig";

            string certificate = null;
            if(args.Length > 0)
            {
                certificate = args[0];
            }

            string ext = string.IsNullOrEmpty(certificate) ? ".xml" : ".enc";

            logger.Info("Starting Middleware process...");
            var config = new MiddlewareConfig(configFile+ext, logger, certificate);

            IMessageStats stats = new MessageStats("dev", config.MaxConnections);
            IAuthenticationHandler authHandler = new DefaultAuthenticationHandler();

            //Initialise authentication server listener and endpoint listener...
            WSServer authServer = InitialiseAuthenticationServer(config, authHandler, stats);
            WSServer server = InitialiseEndpointServer(config, authHandler, stats);

            logger.Log(LogLevel.Info, "server initialised");

            //Wait until user shuts down the application...
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _shutdownEvent.Set();
            };

            _shutdownEvent.WaitOne();

            logger.Log(LogLevel.Info, "shutting down server");

            //cleanup...
            authServer.Stop();
            server.Stop();

        }

        #endregion

        #region Private Methods

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

            var fileRequestManager = new FileRequestManager(config.FileRequestHandlerPath);
            WSServer server = new Endpointserver(manager, stats, fileRequestManager);
            server.Start(config.URLS.Split(','), config.MaxConnections);
            return server;
        }

        private static WSServer InitialiseAuthenticationServer(MiddlewareConfig config,
                                                         IAuthenticationHandler rootAuthHandler,
                                                         IMessageStats stats)
        {
            AuthRequestCache authCache = new AuthRequestCache(config.AuthTimeoutMS);
            var messageHandler = new AuthLoginResponseHandler(authCache);
            messageHandler.AddHandler(new AuthRegisterMessageHandler(rootAuthHandler, authCache));

            logger.Log(LogLevel.Info, "initialising auth endpoint server...");
            EndpointManager manager = new AuthenticationManager(messageHandler, rootAuthHandler, stats);

            var server = new WSServer(manager);
            server.Start(config.AUTHURLS.Split(','), config.MaxAuthConnections);
            return server;
        }

        #endregion

        #region Private Static Data Members

        private static AutoResetEvent _shutdownEvent = new AutoResetEvent(false);
        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
