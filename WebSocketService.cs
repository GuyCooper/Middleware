using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace Middleware
{
    class MisroutingException : MiddlewareException
    {
        public MisroutingException(string id) : base(id, "misrouted message")
        {
        }
    }
    class WsEndpoint : IEndpoint
    {
        private WebSocket _socket; //underlying socket transport
        private IHandler _handler;

        public string Id { get; private set; }

        public WsEndpoint(WebSocket socket, IHandler handler)
        {
            Id = Guid.NewGuid().ToString();
            _socket = socket;
            _handler = handler;
        }

        public void DataReceived(string data)
        {
            Console.WriteLine("data received on endpoint {0}, {1}", Id, data);

            Message message = JsonConvert.DeserializeObject<Message>(data);
            //populate the session id
            message.Source = this;
            message.SourceId = Id;

            //now forward message onto handlers
            if(_handler.ProcessMessage(message) == false)
            {
                Console.WriteLine("invalid command {0}", message.Command);
            }
        }

        private async void _SendDataToClient(string payload)
        {
            var encoded = Encoding.UTF8.GetBytes(payload);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public void SendData(Message message)
        {
            //first ensure that this message is for this endpoint unless it is
            //a broadcast message
            if (message.DestinationId != null && message.DestinationId != Id )
            {
                throw new MisroutingException(Id);
            }

            //ensure that the Source endpoint member is NT serialised
            var payload = JsonConvert.SerializeObject(message);
            _SendDataToClient(payload);
        }

        public void OnError(Message message, string error)
        {
            //do not send responses back to client for broadcast message types
            //i.e. if message type is an update
            if (message.Type == MessageType.UPDATE)
            {
                return;
            }

            //Send error response to client
            var response = new Message
            {
                Type = MessageType.RESPONSE_ERROR,
                RequestId = message.RequestId,
                Payload = error
            };

            var payload = JsonConvert.SerializeObject(response);
            _SendDataToClient(payload);
        }

        public void OnSucess(Message message)
        {
            //do not send responses back to client for broadcast message types
            //i.e. if message type is an update
            if(message.Type == MessageType.UPDATE)
            {
                return;
            }

            //send a success response back to the client
            var response = new Message
            {
                Type = MessageType.RESPONSE_SUCCESS,
                RequestId = message.RequestId
            };
            var payload = JsonConvert.SerializeObject(response);

            _SendDataToClient(payload);
        }

        public void EndpointClosed()
        {
            _handler.RemoveEndpoint(Id);
        }

        public bool MatchEndpoint(WebSocket socket)
        {
            return _socket == socket;
        }
    }

    class EndpointManager
    {
        //private Dictionary<string, Endpoint> _endpointLookup = new Dictionary<string, Endpoint>();
        private List<WsEndpoint> _endpoints = new List<WsEndpoint>();
        private IHandler _handler;

        public EndpointManager(IHandler handler)
        {
            _handler = handler;
        }

        public void NewConnection(WebSocket socket)
        {
            var endpoint = new WsEndpoint(socket, _handler);
            _endpoints.Add(endpoint);
            //_endpointLookup.Add(id, endpoint);
        }

        public void CloseConnection(WebSocket socket)
        {
            var endpoint = _endpoints.Find(x => x.MatchEndpoint(socket));
            if (endpoint != null)
            {
                endpoint.EndpointClosed();
            }
            _endpoints.Remove(endpoint);

        }

        public void DataRecevied(WebSocket socket, string data)
        {
            var endpoint = _endpoints.Find(x => x.MatchEndpoint(socket));
            if (endpoint != null)
            {
                endpoint.DataReceived(data);
            }
        }

        //public void SendData(string id, string data)
        //{

        //}
    }
    class WSServer
    {
        private HttpListener _httpListener;
        private EndpointManager _manager;

        public WSServer(EndpointManager manager)
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

        private async Task _ProcessConnection(HttpListenerContext context)
        {
            if (context.Request.IsWebSocketRequest == true)
            {
                Console.WriteLine("connection received");
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                WebSocket ws = webSocketContext.WebSocket;
                _manager.NewConnection(ws);
                while (ws.State == WebSocketState.Open)
                {
                    string data = await _readDatafromSocket(ws);
                    _manager.DataRecevied(ws, data);
                }
                Console.WriteLine("connection closed");
                _manager.CloseConnection(ws);
            }
            else
            {
                //normal HTTP requests like a web server
                //var data = _readRequest(context);
            }
        }

        public void Start(string url, int maxConnections)
        {
            _httpListener.Prefixes.Add(url);
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
                    await _ProcessConnection(context);
                    sem.Release();
                });
#pragma warning restore 4014
            }
        }

        public void Stop()
        {
            _httpListener.Close();
        }
    }
}


