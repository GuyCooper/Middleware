using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.IO;
using System.Collections.Concurrent;
using NLog;

namespace MiddlewareNetClient
{
    /// <summary>
    /// ISession Inteface. interface define a session to send data to
    /// </summary>
    public interface ISession
    {
        void SendMessage(string message);
    }

    /// <summary>
    /// Websocket implementation of ISession
    /// </summary>
    class WebSocketSession : ISession, IDisposable
    {
        #region Public Methods
        /// <summary>
        /// Constructor
        /// </summary>
        public WebSocketSession(IMiddlewareManager manager, string url)
        {
            _uri = new Uri(url);
            _manager = manager;
            //_connectEvent = new AutoResetEvent(false);
            _sendDataTask = Task.Factory.StartNew(_handleSendRequests);
        }

        /// <summary>
        /// Send data to websocket. add it to send queue
        /// </summary>
        public void SendMessage(string message)
        {
            _sendQueue.Add(message);
        }

        /// <summary>
        /// connect websocket to URI
        /// </summary>
        public async Task Connect()
        {
            if (_ws == null)
            {

                _ws = new ClientWebSocket();

                _ws.Options.SetRequestHeader(Middleware.MessageHeaders.CLIENTLOCATION, System.Environment.MachineName);
                //_ws.Options.SetRequestHeader(Middleware.MessageHeaders.CLIENTUSERNAME, "admin");
                //_ws.Options.SetRequestHeader(Middleware.MessageHeaders.CLIENTPASSWORD, "password");

                await _ws.ConnectAsync(_uri, System.Threading.CancellationToken.None).ContinueWith(async (t) =>
                {
                    try
                    {
                        //_connectEvent.Set();
                        while (_ws.State == WebSocketState.Open)
                        {
                            string data = await _readDatafromSocket(_ws);
                            _manager.OnMessageCallback(this, data);
                        }
                    }
                    catch(Exception ex)
                    {
                        logger.Log(LogLevel.Error, ex);
                    }

                    //connection closed
                });
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Clean up send thread, send queue and websocket
        /// </summary>
        public void Dispose()
        {
            if(IsDisposed == true)
            {
                return;
            }

            ////stop the senddata task thread
            _shutdownSendEvent.Set();
            ////wait for the send thread to shutdown
            _sendDataTask.Wait(5000);
            ////now dispose everything
            _shutdownSendEvent.Dispose();
            _sendQueue.Dispose();
            _sendDataTask.Dispose();

            //now we can dispose the websocket as we know we are no longer trying to write to it
            if (_ws != null)
            {
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "goodbye", System.Threading.CancellationToken.None);
                _ws.Dispose();
                _ws = null;
            }
            IsDisposed = true;
        }

        private bool IsDisposed;

        #endregion

        #region Private Methods

        /// <summary>
        /// Thread handler for sending data to the websocket. Uses BlockingCollection class to
        /// take a message off the queue and send it
        /// </summary>
        private void _handleSendRequests()
        {
            while (_shutdownSendEvent.WaitOne(0) == false)
            {
                string message;
                if(_sendQueue.TryTake(out message, 100) == true)
                {
                    if (_ws.State == WebSocketState.Open)
                    {
                        ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                        var result = _ws.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
                        result.Wait();
                    }
                }
            }
        }

        /// <summary>
        /// Helper method for reading data off the websocket
        /// </summary>
        private async Task<string> _readDatafromSocket(WebSocket ws)
        {
            //now we can start to asyncronsly receive data on this socket
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);
            WebSocketReceiveResult result = null;

            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, System.Threading.CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        #endregion

        #region Private Data Members

        // Websocket connection
        private ClientWebSocket _ws;

        // URI of connection on websocket
        private Uri _uri;

        // IMiddlewareManager wraps a callack for sending data that has been received on this
        //websocket
        private IMiddlewareManager _manager;

        //private AutoResetEvent _connectEvent;

        // event for shutting down the send data thread
        private readonly ManualResetEvent _shutdownSendEvent = new ManualResetEvent(false);

        //task for send data to websocket. 
        private readonly Task _sendDataTask;

        //producer / consumer collection for sending data to websocket. Only a single call to senddataastnc can be
        //made at any time so this allows multiple threads to senddata on this class concurrently
        private readonly BlockingCollection<string> _sendQueue = new BlockingCollection<string>();

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
