using System;
using System.Threading.Tasks;
using System.Threading;
using NLog;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace Middleware
{
    /// <summary>
    /// XML serialisible Class contains configuration parameters for middleware process.
    /// </summary>
    [XmlRoot(ElementName = "configuration")]
    public class ConfigurationSettings
    {
        #region Public Properties

        [XmlElement("hosturls")]
        public string URLS { get; set; }
        [XmlElement("authurls")]
        public string AUTHURLS { get; set; }
        [XmlElement("maxconnections")]
        public int MaxConnections { get; set; }
        [XmlElement("maxauthconnections")]
        public int MaxAuthConnections { get; set; }
        [XmlElement("authtimeoutms")]
        public int AuthTimeoutMS { get; set; }
        [XmlElement("filehandlerpath")]
        public string FileRequestHandlerPath { get; set; }

        #endregion

    }
    /// <summary>
    /// MiddlewareConfig class. Parses the configuration file.
    /// </summary>
    class MiddlewareConfig
    {
        #region Constructor

        /// <summary>
        /// Constructor. Parse the config file
        /// </summary>
        public MiddlewareConfig(string filename, Logger logger)
        {
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                XmlSerializer serialiser = new XmlSerializer(typeof(ConfigurationSettings));
                _configuration = (ConfigurationSettings)serialiser.Deserialize(fs);
            }

            //log the parameters
            var properties = _configuration.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance);
            foreach(var property in properties)
            {
                logger.Info($"{property.Name} : {property.GetValue(_configuration)}");
            }
        }

        /// <summary>
        /// Constructor. Parse the command line parameters
        /// </summary>
        //public MiddlewareConfig(string[] args)
        //{
        //    ConfigParser parser = new ConfigParser();
        //    parser.AddParameter("H", "server url", "http://localhost:8080/MWARE/", (val) => { this.URLS = val; });
        //    //parser.AddParameter("H", "server url","https://localhost:8443/", (val) => { this.URLS = val; });
        //    parser.AddParameter("S", "auth url", "http://localhost:9092/", (val) => { this.AUTHURLS = val; });
        //    parser.AddParameter("M", "max client connections", "10", (val) => { this.MaxConnections = int.Parse(val); });
        //    parser.AddParameter("N", "max auth client connections", "1", (val) => { this.MaxAuthConnections = int.Parse(val); });
        //    parser.AddParameter("R", "root folder", @"C:\Projects\Middleware\Middleware", (val) => { this.RootFolder = val; });
        //    parser.AddParameter("T", "authg response timeout", "30000", (val) => { this.AuthTimeout = int.Parse(val); });

        //    parser.ParseCommandLine(args);
        //    parser.LogValues();
        //}

        #endregion

        #region public properties

        public string URLS { get { return _configuration.URLS; } }
        public string AUTHURLS { get { return _configuration.AUTHURLS; } }
        public int MaxConnections { get { return _configuration.MaxConnections; } }
        public int MaxAuthConnections { get { return _configuration.MaxAuthConnections; } }
        public int AuthTimeoutMS { get { return _configuration.AuthTimeoutMS; } }
        public string FileRequestHandlerPath { get { return _configuration.FileRequestHandlerPath; } }
        #endregion

        #region Private Data

        private ConfigurationSettings _configuration;

        #endregion

    }

    /// <summary>
    /// Main Static Program class.
    /// </summary>
    class Program
    {
        #region Main Entry Point

        /// <summary>
        /// Main Entry point into process.
        /// </summary>
        static void Main(string[] args)
        {
            logger.Info("Starting Middleware process...");
            var config = new MiddlewareConfig("MiddlewareConfig.xml", logger);

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
