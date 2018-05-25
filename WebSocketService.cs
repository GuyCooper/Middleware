using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.IO;
using System.Threading;
using NLog;

namespace Middleware
{
    /// <summary>
    /// Custom exception class for misroutes
    /// </summary>
    class MisroutingException : MiddlewareException
    {
        public MisroutingException(string id) : base(id, "misrouted message")
        {
        }
    }

    /// <summary>
    /// Connection class for a Websocket connection.
    /// </summary>
    class WebsocketConnection : IConnection
    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public WebsocketConnection(WebSocket socket)
        {
            _socket = socket;
        }

        /// <summary>
        /// Sed data on this connection
        /// </summary>
        public async void SendData(string data)
        {
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

            await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Compares this connection to another
        /// </summary>
        public bool MatchConnection(WebSocket socket)
        {
            return _socket == socket;
        }

        #endregion

        #region Private Data Members
        private WebSocket _socket;

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }

    /// <summary>
    /// Interface defines a class that handles calls from a wsserver web socket
    /// connection
    /// </summary>
    interface ISocketManager
    {
        void NewConnection(WebSocket socket, string origin);
        void CloseConnection(WebSocket socket);
        void DataRecevied(WebSocket socket, string data);
    }

    /// <summary>
    /// class is an ISocketManager that maintains the concept of
    /// endpoints. that is, each connection instance is a seperate
    /// endpoint where each endpoint has a handler chain as defined
    /// in the IEndpoint interface
    /// </summary>
    class EndpointManager : ISocketManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public EndpointManager(IMessageHandler handler, IAuthenticationHandler authHandler, IMessageStats stats)
        {
            _handler = handler;
            _authHandler = authHandler;
            _stats = stats;
        }

        /// <summary>
        /// Method called when a new connection is established
        /// </summary>
        public void NewConnection(WebSocket socket, string origin)
        {
            var connection = new WebsocketConnection(socket);
            var endpoint = new MiddlewareEndpoint(connection, _handler, _authHandler);
            _endpoints.Add(connection, endpoint);
        }

        /// <summary>
        /// Method called when a connection is closed remotely.
        /// </summary>
        public void CloseConnection(WebSocket socket)
        {
            var connection = _LookupConnection(socket);
            if(connection != null)
            {
                var endpoint = _endpoints[connection];
                endpoint.EndpointClosed();
                _endpoints.Remove(connection);
                LogCloseConnection(endpoint);
            }
        }

        /// <summary>
        /// Method called when data is received on a connection.
        /// </summary>
        public void DataRecevied(WebSocket socket, string data)
        {
            var connection = _LookupConnection(socket);
            if (connection != null)
            {
                var endpoint = _endpoints[connection];
                logger.Log(LogLevel.Trace, $"DataRecevied on endpoint {endpoint.Id}. data: {data}.");

                if (endpoint.Authenticated == false)
                {
                    logger.Log(LogLevel.Trace, $"Authenticating endpoint {endpoint.Id}...");
                    endpoint.AuthenticateEndpoint(data).ContinueWith(t =>
                   {
                       AuthResponse response = t.Result;
                       AuthResult authResult = response.Result;
                       if (authResult.Success == false)
                       {
                           //authentication failed,
                           logger.Log(LogLevel.Error, $"Authentication failed on endpoint {endpoint.Id}: {authResult.Message}. Removing endpoint.");
                           _endpoints.Remove(connection);
                       }
                       else
                       {
                           logger.Log(LogLevel.Trace, $"Authentication succeded on endpoint {endpoint.Id}.");
                           var payload = response.Payload;
                           LogNewConnection(endpoint, payload);
                       }
                   });
                }
                else
                {
                    logger.Log(LogLevel.Trace, $"Data reeived on endpoint {endpoint.Id}. Processing request...");
                    endpoint.DataReceived(data);
                }
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Log a new connection with the statistics manager.
        /// </summary>
        protected virtual void LogNewConnection(IEndpoint endpoint, LoginPayload payload)
        {
            _stats.NewConnection(endpoint.Id,
                                payload.Source,
                                payload.AppName,
                                payload.Version, false);
        }

        /// <summary>
        /// Log a close connection with the statistics manager.
        /// </summary>
        protected virtual void LogCloseConnection(IEndpoint endpoint)
        {
            _stats.CloseConnection(endpoint.Id, false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Helper method for looking up a connection object using the underlying websocket
        /// </summary>
        private WebsocketConnection _LookupConnection(WebSocket socket)
        {
            return _endpoints.Keys.FirstOrDefault(conn =>
            {
                return conn.MatchConnection(socket);
            });
        }

        #endregion

        #region Private Data Members

        private readonly Dictionary<WebsocketConnection, IEndpoint> _endpoints = new Dictionary<WebsocketConnection, IEndpoint>();
        private readonly IMessageHandler _handler;
        private readonly IAuthenticationHandler _authHandler;
        protected readonly IMessageStats _stats;

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }

    /// <summary>
    /// web socket server class encapsulates a websocket connection
    /// </summary>
    class WSServer
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public WSServer(ISocketManager manager)
        {
            _httpListener = new HttpListener();
            _manager = manager;
        }

        /// <summary>
        /// Start listening for connections.
        /// </summary>
        public void Start(string[] urls, int maxConnections)
        {
            foreach (var url in urls)
            {
                _httpListener.Prefixes.Add(url);
            }

            _httpListener.Start();

            var sem = new Semaphore(maxConnections, maxConnections);
            while (true)
            {
                sem.WaitOne();
#pragma warning disable 4014
                //var context = await _httpListener.GetContextAsync();
                _httpListener.GetContextAsync().ContinueWith(async (t) =>
                {
                    var context = await t;
                    _ProcessConnection(context);
                    sem.Release();
                });
#pragma warning restore 4014
            }
        }

        /// <summary>
        /// Stop listening for connections
        /// </summary>
        public void Stop()
        {
            _httpListener.Close();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Read data from a connection.
        /// </summary>
        private async Task<string> _readDatafromSocket(WebSocket ws)
        {
            //now we can start to asyncronsly receive data on this socket
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);
            WebSocketReceiveResult result = null;

            int totalRead = 0;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    totalRead += result.Count;
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                logger.Log(LogLevel.Trace, $"Total bytes read from socket: {totalRead}");
                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Retrieve the header data from a socket request.
        /// </summary>
        private string GetHeaderValue(HttpListenerRequest context, string key, string defaultVal)
        {
            foreach (var name in context.Headers.AllKeys)
            {
                var header = context.Headers.GetValues(name);
                logger.Log(LogLevel.Trace, $"Header name: {name}, value: {header[0]}");
            }

            var vals = context.Headers.GetValues(key);
            return (vals != null) && (vals.Length > 0) ? vals[0] : defaultVal;
        }

        /// <summary>
        /// Read the authentication parameters from the header of a socket request
        /// </summary>
        private string GetDecryptedHeaderValue(HttpListenerRequest context, string key)
        {
            var headerVal = GetHeaderValue(context, key, "");
            if(string.IsNullOrEmpty(headerVal) == false)
            {
                //always assume UTF8 encoding
                var rawVal = Convert.FromBase64String(headerVal);
                return System.Text.Encoding.UTF8.GetString(rawVal);
            }
            return headerVal;
        }

        /// <summary>
        /// Method called when a new connection is received on the listening socket.
        /// </summary>
        private async void _ProcessConnection(HttpListenerContext context)
        {
            var authKey = GetDecryptedHeaderValue(context.Request, MessageHeaders.AUTHENTICATION_KEY);
            if (context.Request.IsWebSocketRequest == true)
            {
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                logger.Log(LogLevel.Info, "Websocket Connection received.");

                WebSocket ws = webSocketContext.WebSocket;
                var origin = GetHeaderValue(context.Request, MessageHeaders.CLIENTLOCATION, webSocketContext.Origin);
                _manager.NewConnection(ws, origin);
                while (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        string data = await _readDatafromSocket(ws);
                        _manager.DataRecevied(ws, data);
                    }
                    catch (Exception ex)
                    {
                        logger.Log(LogLevel.Error, ex);
                    }
                }
                logger.Log(LogLevel.Info, "Connection closed.");
                _manager.CloseConnection(ws);
            }
            else
            {
                _ProcessHttpRequest(context);
            }
        }

        protected virtual void _ProcessHttpRequest(HttpListenerContext context) { }

        #endregion

        #region Private Data Members

        private HttpListener _httpListener;
        private readonly ISocketManager _manager;

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }

    /// <summary>
    /// class encapsulates an endpoint service connection, this is an
    /// extended web socket connection that also handles http requests
    /// to make it a complete web server
    /// </summary>
    class Endpointserver : WSServer
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public Endpointserver(ISocketManager manager, string root, IMessageStats stats)  :
            base(manager)
        {
            _root = root;
            _stats = stats;
        }

        /// <summary>
        /// Process a http request. If an index request, return the statistics page.
        /// </summary>
        protected override void _ProcessHttpRequest(HttpListenerContext context)
        {
            //standard http request. if url spcified then open file from fs
            //othwerwise return full stats page
            logger.Log(LogLevel.Info, $"processing http request: {context.Request.RawUrl}");
            string result = null;
            if (context.Request.RawUrl != "/")
            {
                try
                {
                    var filename = Path.Combine(_root, context.Request.RawUrl.Trim('/'));
                    using (var indexFile = new StreamReader(filename))
                    {
                        result = indexFile.ReadToEnd();
                    }
                }
                catch (FileNotFoundException) { }
            }
            else
            {
                result = _stats.ToXML();
            }
            if (result != null)
            {
                result += "\n";
                var encoded = Encoding.UTF8.GetBytes(result);
                //await context.Response.OutputStream.WriteAsync(encoded, 0, encoded.Length);
                context.Response.Close(encoded, false);
            }
        }

        #endregion

        #region Private Data Members

        private string _root;
        private IMessageStats _stats;

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();


        #endregion
    }

    /// <summary>
    /// Class handles authentication clients for the server
    /// </summary>
    class AuthenticationManager : EndpointManager
    {
        public AuthenticationManager(IMessageHandler handler, IAuthenticationHandler authHandler, IMessageStats stats) : base(handler, authHandler, stats)
        {
        }

        protected override void LogNewConnection(IEndpoint endpoint, LoginPayload payload)
        {
            _stats.NewConnection(endpoint.Id,
                                payload.Source,
                                payload.AppName,
                                payload.Version, true);
        }

        protected override void LogCloseConnection(IEndpoint endpoint)
        {
            _stats.CloseConnection(endpoint.Id, true);
        }
    }
}


