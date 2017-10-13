using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.IO;

namespace MiddlewareNetClient
{
    public interface ISession
    {
        void SendMessage(string message);
        void StartDispatcher();
    }

    class WebSocketSession : ISession, IDisposable
    {
        private ClientWebSocket _ws;
        private Uri _uri;
        private IMiddlewareManager _manager;

        public WebSocketSession(IMiddlewareManager manager, string url)
        {
            _uri = new Uri(url);
            _manager = manager;
        }

        private async void Connect()
        {
            if (_ws == null)
            {
                _ws = new ClientWebSocket();
                _ws.Options.SetRequestHeader("clientLocation", "here");

                await _ws.ConnectAsync(_uri, System.Threading.CancellationToken.None).ContinueWith(async (t) =>
                {
                    while (_ws.State == WebSocketState.Open)
                    {
                        string data = await _readDatafromSocket(_ws);
                        _manager.OnMessageCallback(this, data);
                    }

                    //connection closed
                });
            }
        }

        public void Dispose()
        {
            if (_ws != null)
            {
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "goodbye", System.Threading.CancellationToken.None);
                _ws.Dispose();
                _ws = null;
            }
        }

        public async void SendMessage(string message)
        {
            if(_ws == null)
            {
                throw new ApplicationException("must call StartDispatcher berfore sending data");
            }

            if(_ws.State == WebSocketState.Open)
            {
                ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                await _ws.SendAsync(bytesToSend, WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            }
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
                    result = await ws.ReceiveAsync(buffer, System.Threading.CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        public void StartDispatcher()
        {
            Connect();
        }
    }
}
