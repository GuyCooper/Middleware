using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.IO;
using System.Threading;

namespace Middleware
{
    class MisroutingException : MiddlewareException
    {
        public MisroutingException(string id) : base(id, "misrouted message")
        {
        }
    }

    class WebsocketConnection : IConnection
    {
        private WebSocket _socket;
        public WebsocketConnection(WebSocket socket)
        {
            _socket = socket;
        }

        public async Task SendData(string data)
        {
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public bool MatchConnection(WebSocket socket)
        {
            return _socket == socket;
        }
    }
   
    /// <summary>
    /// interface defines a class that handles calls from a wsserver web socket
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
        //private Dictionary<string, Endpoint> _endpointLookup = new Dictionary<string, Endpoint>();
        private Dictionary<WebsocketConnection, IEndpoint> _endpoints = new Dictionary<WebsocketConnection, IEndpoint>();
        private IHandler _handler;
        private IAuthenitcationHandler _authHandler;
        private IMessageStats _stats;

        public EndpointManager(IHandler handler, IAuthenitcationHandler authHandler, IMessageStats stats)
        {
            _handler = handler;
            _authHandler = authHandler;
            _stats = stats;
        }

        private WebsocketConnection _LookupConnection(WebSocket socket)
        {
            return _endpoints.Keys.FirstOrDefault(conn =>
            {
                return conn.MatchConnection(socket);
            });
        }

        public void NewConnection(WebSocket socket, string origin)
        {
            var connection = new WebsocketConnection(socket);
            var endpoint = new MiddlewareEndpoint(connection, _handler, _authHandler);
            _endpoints.Add(connection, endpoint);
            _stats.OpenConnection(endpoint.Id, origin);
        }

        public void CloseConnection(WebSocket socket)
        {
            var connection = _LookupConnection(socket);
            if(connection != null)
            {
                var endpoint = _endpoints[connection];
                endpoint.EndpointClosed();
                _endpoints.Remove(connection);
                _stats.CloseConnection(endpoint.Id);
            }
        }

        public void DataRecevied(WebSocket socket, string data)
        {
            var connection = _LookupConnection(socket);
            if (connection != null)
            {
                var endpoint = _endpoints[connection];
                if(endpoint.Authenticated == false)
                {
                    endpoint.AuthenticateEndpoint(data).ContinueWith(t =>
                   {
                       if(t.Result == false)
                       {
                           //authentication failed,
                           Console.WriteLine("authentication failed!!. removing endpoint");
                           _endpoints.Remove(connection);
                       }
                       Console.WriteLine("authentication succeded!");
                   });
                }
                else
                {
                    endpoint.DataReceived(data);
                }
            }
        }
    }

    /// <summary>
    /// web socket server class encapsulates a websocket connection
    /// </summary>
    class WSServer
    {
        private HttpListener _httpListener;
        private ISocketManager _manager;

        public WSServer(ISocketManager manager)
        {
            _httpListener = new HttpListener();
            _manager = manager;
        }

        //read the data from the web socket request
        private string _readRequest(HttpListenerContext context)
        {
            string ret = null;
            //read the websocket request from the client
            using (Stream body = context.Request.InputStream)
            {
                Encoding encoding = context.Request.ContentEncoding;
                using (StreamReader reader = new System.IO.StreamReader(body, encoding))
                {
                    if (context.Request.ContentType != null)
                    {
                        Console.WriteLine("Client data content type {0}", context.Request.ContentType);
                    }
                    Console.WriteLine("Client data content length {0}", context.Request.ContentLength64);

                    Console.WriteLine("Start of client data:");
                    // Convert the data to a string and display it on the console.
                    ret = reader.ReadToEnd();
                    Console.WriteLine(ret);
                    Console.WriteLine("End of client data:");
                }
            }
            return ret;
        }

        private async Task<string> _readDatafromSocket(WebSocket ws)
        {
            //now we can start to asyncronsly receive data on this socket
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);
            WebSocketReceiveResult result = null;

            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private string GetHeaderValue(HttpListenerRequest context, string key, string defaultVal)
        {
            foreach (var name in context.Headers.AllKeys)
            {
                var header = context.Headers.GetValues(name);
                Console.WriteLine("Header name: {0}, value: {1}", name, header[0]);
            }

            //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(convert);
            //var str = System.Text.Encoding.UTF8.GetString(result);
            //Convert.FromBase64String();
            //Convert.ToBase64String();

            var vals = context.Headers.GetValues(key);
            return (vals != null) && (vals.Length > 0) ? vals[0] : defaultVal;
        }

        private string GetDecryptedHeaderValue(HttpListenerRequest context, string key)
        {
            var headerVal = GetHeaderValue(context, key, "");
            if(string.IsNullOrEmpty(headerVal) == false)
            {
                //always assume UTF8 encoding
                var rawVal = Convert.FromBase64String(headerVal);
                return System.Text.Encoding.UTF8.GetString(rawVal);
                //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(headerVal);
                //var str = System.Text.Encoding.UTF8.GetString(result);
                //Convert.FromBase64String();
                //Convert.ToBase64String();
            }
            return headerVal;
        }

        private async void _ProcessConnection(HttpListenerContext context)
        {
            var authKey = GetDecryptedHeaderValue(context.Request, MessageHeaders.AUTHENTICATION_KEY);
            if (context.Request.IsWebSocketRequest == true)
            {
                Console.WriteLine("connection received");
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                WebSocket ws = webSocketContext.WebSocket;
                var origin = GetHeaderValue(context.Request, MessageHeaders.CLIENTLOCATION, webSocketContext.Origin);
                _manager.NewConnection(ws, origin);
                while (ws.State == WebSocketState.Open)
                {
                    string data = await _readDatafromSocket(ws);
                    try
                    {
                        _manager.DataRecevied(ws, data);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error handling received data: {0}", e.Message);
                    }
                }
                Console.WriteLine("connection closed");
                _manager.CloseConnection(ws);
            }
            else
            {
                _ProcessHttpRequest(context);
            }
        }

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

        public void Stop()
        {
            _httpListener.Close();
        }

        protected virtual void _ProcessHttpRequest(HttpListenerContext context) { }

    }

    /// <summary>
    /// class encapsulates an endpoint service connection, this is an
    /// extended web socket connection that also handles http requests
    /// to make it a complete web server
    /// </summary>
    class Endpointserver : WSServer
    {
        private string _root;
        private IMessageStats _stats;

        public Endpointserver(ISocketManager manager, string root, IMessageStats stats)  :
            base(manager)
        {
            _root = root;
            _stats = stats;
        }

        protected override void _ProcessHttpRequest(HttpListenerContext context)
        {
            //standard http request. if url spcified then open file from fs
            //othwerwise return full stats page
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
    }

    /// <summary>
    /// class handles authentication clients for the server
    /// </summary>
    class AuthenticationManager : ISocketManager
    {
        public void CloseConnection(WebSocket socket)
        {
            throw new NotImplementedException();
        }

        public void DataRecevied(WebSocket socket, string data)
        {
            throw new NotImplementedException();
        }

        public void NewConnection(WebSocket socket, string origin)
        {
            throw new NotImplementedException();
        }
    }
}


