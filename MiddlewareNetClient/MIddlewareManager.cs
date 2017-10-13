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

    public class MiddlewareRequestParams
    {
        public MiddlewareRequestParams(string channel, SendDataCallback success, SendDataCallback fail )
        {
            Channel = channel;
            OnSuccess = success;
            OnFail = fail;
        }

        public string Channel { get; private set; }
        public SendDataCallback OnSuccess { get; private set; }
        public SendDataCallback OnFail { get; private set; }
    };

    public class MiddlewareManager : IMiddlewareManager
    {
        private Dictionary<string, MiddlewareRequestParams> currentRequestsList_ = new Dictionary<string, MiddlewareRequestParams>();
        private HandleData _messageCallbackHandler = null;

        public bool SubscribeToChannel(ISession session, MiddlewareRequestParams prms)
        {
            return _RequestImp(session, prms, HandlerNames.SUBSCRIBETOCHANNEL, MessageType.REQUEST, "", "");
        }

        public bool SendMessageToChannel(ISession session, MiddlewareRequestParams prms, string payload, string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("must specify a valid destination for sendMessage");
            }

            return _RequestImp(session, prms, HandlerNames.SENDMESSAGE, MessageType.UPDATE, payload, destination);
        }

	    public bool AddChannelListener(ISession session, MiddlewareRequestParams prms)
        {
            return _RequestImp(session, prms, HandlerNames.ADDLISTENER, MessageType.REQUEST,  "", "");
        }

        public bool SendRequest(ISession session, MiddlewareRequestParams prms, string payload)
        {
            return _RequestImp(session, prms, HandlerNames.SENDREQUEST, MessageType.REQUEST, payload, "");
        }

	    public bool PublishMessage(ISession session, MiddlewareRequestParams prms, string payload)
        {
            return _RequestImp(session, prms, HandlerNames.PUBLISHMESSAGE, MessageType.UPDATE, payload, "");
        }

	    public ISession  CreateSession(string url)
        {
            return new WebSocketSession(this, url);
        }

        public void RegisterMessageCallbackFunction(HandleData msgCallback)
        {
            _messageCallbackHandler = msgCallback;
        }

        private bool _RequestImp(ISession session, MiddlewareRequestParams prms, string command, MessageType type, string payload, string destination)
        {
            var msg = new Message
            {
                Channel = prms.Channel,
                Command = command,
                DestinationId = destination,
                Payload = payload,
                Type = type,
                RequestId = Guid.NewGuid().ToString()
            };

            currentRequestsList_.Add(msg.RequestId, prms);

            if (session != null)
            {
                var serialised =  JsonConvert.SerializeObject(msg);
                session.SendMessage(serialised);
                return true;
            }
            return false;
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

            MiddlewareRequestParams prms;
            if(currentRequestsList_.TryGetValue(msg.RequestId, out prms) == true)
            {
                if(msg.Type == MessageType.RESPONSE_SUCCESS)
                {
                    prms.OnSuccess?.Invoke(session, msg.Payload);
                }
                else if (msg.Type == MessageType.RESPONSE_ERROR)
                {
                    prms.OnFail?.Invoke(session, msg.Payload);
                }
                currentRequestsList_.Remove(msg.RequestId);
            }
        }

        public void OnConnectionClosed(ISession session)
        {

        }
    }
}
