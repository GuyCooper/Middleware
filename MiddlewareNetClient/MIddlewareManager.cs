using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Middleware;
using Newtonsoft.Json;

namespace MiddlewareNetClient
{
    public delegate void SendDataCallback(ISession session, string data);
    public delegate void HandleData(ISession session, Message message);

    public interface IMiddlewareManager
    {
        void OnMessageCallback(ISession session, string data);
        void OnConnectionClosed(ISession session);
    }

    public interface ILogger
    {
        void LogError(string error);
        void LogMessage(string message);
    }

    public class SendDataResponse
    {
        public void Update(ISession session, string payload, bool success)
        {
            Session = session;
            Payload = payload;
            Success = success;
        }

        public ISession Session { get; private set; }
        public string Payload { get; private set; }
        public bool Success { get; set; }
    }

    public class MiddlewareManager : IMiddlewareManager
    {
        private readonly string VERSION = "1.0";
        private readonly string APPNAME = "NET Client Library";

        private Dictionary<string, Tuple<SendDataResponse, Task<SendDataResponse>>> currentRequestsList_ = new Dictionary<string, Tuple<SendDataResponse, Task<SendDataResponse>>>();

        private HandleData _messageCallbackHandler = null;

        public Task<SendDataResponse> SubscribeToChannel(ISession session, string channel)
        {
            return _RequestImp(session, channel, HandlerNames.SUBSCRIBETOCHANNEL, "", "");
        }

        public void SendMessageToChannel(ISession session, string channel, string payload, string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("must specify a valid destination for sendMessage");
            }

            _PublishImp(session, channel, HandlerNames.SENDMESSAGE, payload, destination);
        }

	    public Task<SendDataResponse> AddChannelListener(ISession session, string channel)
        {
            return _RequestImp(session, channel, HandlerNames.ADDLISTENER, "", "");
        }

        public Task<SendDataResponse> SendRequest(ISession session, string channel, string payload)
        {
            return _RequestImp(session, channel, HandlerNames.SENDREQUEST, payload, "");
        }

	    public void PublishMessage(ISession session, string channel, string payload)
        {
            _PublishImp(session, channel, HandlerNames.PUBLISHMESSAGE, payload, "");
        }

	    public async Task<ISession> CreateSession(string url, string username, string password, ILogger logger)
        {
            var session =  new WebSocketSession(this, url);
            //first connect to the server
            await session.Connect();

            //send login request otherwise cannot use connection
            var login = new Middleware.LoginPayload
            {
                UserName = username,
                Password = password,
                Source = System.Environment.MachineName,
                AppName = APPNAME,
                Version = VERSION,
            };

            var response = await _RequestImp(session, "LOGIN", HandlerNames.LOGIN, JsonConvert.SerializeObject(login), null);
            if(response.Success == true)
            {
                logger.LogMessage(string.Format("Connect success. {0}", response.Payload));
                return session;
            }
            else
            {
                logger.LogError(string.Format("Connect failed. {0}", response.Payload));
                return null;
            }
        }

        public void RegisterMessageCallbackFunction(HandleData msgCallback)
        {
            _messageCallbackHandler = msgCallback;
        }

        private Message _CreateMessage(string channel, string command, string payload, string destination, MessageType type)
        {
            return new Message
            {
                Channel = channel,
                Command = command,
                DestinationId = destination,
                Payload = payload,
                Type = type,
                RequestId = Guid.NewGuid().ToString()
            };
        }

        private void _SendMessageImpl(ISession session, Message message)
        {
            var serialised = JsonConvert.SerializeObject(message);
            session.SendMessage(serialised);
        }

        private void _PublishImp(ISession session, string channel, string command, string payload, string destination)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session is null!!");
            }
            var message = _CreateMessage(channel, command, payload, destination, MessageType.UPDATE);
            _SendMessageImpl(session, message);
        }

        private Task<SendDataResponse> _RequestImp(ISession session, string channel, string command, string payload, string destination)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session is null!!");
            }

            var message = _CreateMessage(channel, command, payload, destination, MessageType.REQUEST);

            var response = new SendDataResponse();
            var task = new Task<SendDataResponse>(() => { return response; });
            currentRequestsList_.Add(message.RequestId, new Tuple<SendDataResponse, Task<SendDataResponse>>(response, task));
            _SendMessageImpl(session, message);
            return task;
        }

        public void OnMessageCallback(ISession session, string data)
        {
            //deserailise data into a Message object
            Message msg = JsonConvert.DeserializeObject<Message>(data);

            if (msg.Type == MessageType.REQUEST || msg.Type == MessageType.UPDATE)
            {
                //just send message to client
                _messageCallbackHandler?.Invoke(session, msg);
                return;
            }

            Tuple< SendDataResponse, Task<SendDataResponse>> entry;
            if(currentRequestsList_.TryGetValue(msg.RequestId, out entry) == true)
            {
                if(msg.Type == MessageType.RESPONSE_SUCCESS)
                {
                    entry.Item1.Update(session, msg.Payload, true);
                    entry.Item2.Start();
                }
                else if (msg.Type == MessageType.RESPONSE_ERROR)
                {
                    entry.Item1.Update(session, msg.Payload, false);
                    entry.Item2.Start();

                }
                currentRequestsList_.Remove(msg.RequestId);
            }
        }

        public void OnConnectionClosed(ISession session)
        {

        }
    }
}
