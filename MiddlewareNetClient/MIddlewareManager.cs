using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Middleware;
using Newtonsoft.Json;
using NLog;
using MiddlewareInterfaces;

namespace MiddlewareNetClient
{
    public delegate void SendDataCallback(ISession session, string data);
    public delegate void HandleData(ISession session, Message message);

    /// <summary>
    /// Interface for Middleware client
    /// </summary>
    public interface IMiddlewareManager
    {
        void OnMessageCallback(ISession session, byte[] data);
        void OnConnectionClosed(ISession session);
    }

    /// <summary>
    /// Response of a request.
    /// </summary>
    public class SendDataResponse
    {
        public void Update(ISession session, string payload, string requestId,  bool success)
        {
            Session = session;
            Payload = payload;
            Success = success;
            RequestId = requestId;
        }

        public ISession Session { get; private set; }
        public string Payload { get; private set; }
        public bool Success { get; set; }
        public string RequestId { get; private set; }
    }

    /// <summary>
    /// Manages all client middleware communication.
    /// </summary>
    public class MiddlewareManager : IMiddlewareManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public Task<SendDataResponse> SubscribeToChannel(ISession session, string channel)
        {
            return _RequestImp(session, channel, HandlerNames.SUBSCRIBETOCHANNEL, null, "", "", null);
        }

        /// <summary>
        /// SendMessageToChannel. This is called by a channel listener usually in response to a request
        // on a channel. Message is always sent to a single recipient.
        /// </summary>
        public void SendMessageToChannel(ISession session, string channel, string payload, string destination, string requestId, byte[] binaryPayload = null)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("Must specify a valid destination for sendMessage");
            }

            _PublishImp(session, channel, HandlerNames.SENDMESSAGE, payload, destination, requestId, binaryPayload);
        }

        /// <summary>
        /// AddChannelListener. Asynchronous method makes a request to set this client as a listener
        /// on the specified channel. his means that this client will handle all requests made to this
        /// channel. response defines idf this request was successful or not
        /// </summary>
	    public Task<SendDataResponse> AddChannelListener(ISession session, string channel)
        {
            return _RequestImp(session, channel, HandlerNames.ADDLISTENER, null, "", "", null);
        }

        /// <summary>
        /// Send a request to the specified channel. The request will be handled by the channel listener
        /// as defined in the previous method. Asynchronus method. response defines if call was successful
        /// or not.
        /// </summary>
        public Task<SendDataResponse> SendRequest(ISession session, string channel, string payload, byte[] binaryPayload = null)
        {
            return _RequestImp(session, channel, HandlerNames.SENDREQUEST, payload, "", "", null);
        }

        /// <summary>
        /// Register this client as an authentication handler for the middleware service. Payload 
        /// must contain the login details for this client
        /// </summary>
        public Task<SendDataResponse> RegisterAuthHandler(ISession session, string payload)
        {
            return _RequestImp(session, "", HandlerNames.REGISTER_AUTH, payload, "", "", null);
        }

        /// <summary>
        /// Called by a client who has registered as an authenitcation handler in response to an
        /// authenitcation request from another client. AuthResult contains the result of the
        /// authneitcation request.
        /// </summary>
        public void SendAuthenticationResponse(ISession session, string responseID, AuthResult result)
        {
            var message = new Message
            {
                Command = HandlerNames.LOGIN,
                Type = MessageType.REQUEST,
                RequestId = responseID,
                Payload = MiddlewareUtils.SerialiseObjectToString(result)
            };

            _SendMessageImpl(session, message);
        }

        /// <summary>
        /// Publish a message to the specified channel. Message will be received by all clients who
        /// have subscribed to this channel.
        /// </summary>
        public void PublishMessage(ISession session, string channel, string payload, byte[] binaryPayload = null)
        {
            _PublishImp(session, channel, HandlerNames.PUBLISHMESSAGE, payload, "", "", binaryPayload);
        }

        /// <summary>
        /// Create a client session on the middleware. This method must be called before any other
        /// calls can be made on this interface. Asynchronous request. response contains an ISession
        /// object to use for all requests to the middleware. session is null if request failed.
        /// </summary>
	    public async Task<ISession> CreateSession(string url, string username, string password, string appName)
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
                AppName = appName??APPNAME,
                Version = VERSION,
            };

            var response = await _RequestImp(session, "LOGIN", HandlerNames.LOGIN, MiddlewareUtils.SerialiseObjectToString(login), null, null, null);
            if(response.Success == true)
            {
                logger.Log(LogLevel.Info, $"Connect success. {response.Payload}.");
                return session;
            }
            else
            {
                logger.Log(LogLevel.Error, $"Connect failed. {response.Payload}.");
                return null;
            }
        }

        /// <summary>
        /// Register a callback method to handle all callbacks to this client.
        /// </summary>
        /// <param name="msgCallback"></param>
        public void RegisterMessageCallbackFunction(HandleData msgCallback)
        {
            _messageCallbackHandler = msgCallback;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Helper method for creating a Message object. generates a unique request id for message
        /// </summary>
        private Message _CreateMessage(string channel, string command, string payload, string destination, MessageType type, string requestId, byte[] binaryPayload)
        {
            return new Message
            {
                Channel = channel,
                Command = command,
                DestinationId = destination,
                Payload = payload,
                Type = type,
                RequestId = string.IsNullOrEmpty(requestId) ?  Guid.NewGuid().ToString() : requestId,
                BinaryPayload = binaryPayload
            };
        }

        /// <summary>
        /// Helper method for sending a message to the server.
        /// </summary>
        private void _SendMessageImpl(ISession session, Message message)
        {
            var serialised = MiddlewareUtils.SerialiseObject(message);
            session.SendMessage(serialised);
        }

        /// <summary>
        /// Helper method for processing an update message.
        /// </summary>
        private void _PublishImp(ISession session, string channel, string command, string payload, string destination, string requestId, byte[]  binaryPayload)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session is null!!");
            }

            var message = _CreateMessage(channel, command, payload, destination, MessageType.UPDATE, requestId, binaryPayload);
            _SendMessageImpl(session, message);
        }

        /// <summary>
        /// Helper method for processing a message request. message is added to a request queue so it can be
        /// matched with a response when it happens.
        /// </summary>
        private Task<SendDataResponse> _RequestImp(ISession session, string channel, string command, string payload, string destination, string requestId, byte[] binaryPayload)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session is null!!");
            }

            var message = _CreateMessage(channel, command, payload, destination, MessageType.REQUEST, requestId, binaryPayload);

            var response = new SendDataResponse();
            var task = new Task<SendDataResponse>(() => { return response; });
            currentRequestsList_.Add(message.RequestId, new Tuple<SendDataResponse, Task<SendDataResponse>>(response, task));
            _SendMessageImpl(session, message);
            return task;
        }

        /// <summary>
        /// Method called when data is received from the server.
        /// </summary>
        public void OnMessageCallback(ISession session, byte[] data)
        {
            logger.Log(LogLevel.Trace, $"data received: {data.Length}");
            //deserailise data into a Message object
            Message msg = MiddlewareUtils.DeserialiseObject<Message>(data);

            if (msg.Type == MessageType.REQUEST || msg.Type == MessageType.UPDATE)
            {
                //just send message to client
                _messageCallbackHandler?.Invoke(session, msg);
                return;
            }

            //try and map the message to a request. If mapped, invoke the task
            //stored with the request
            Tuple< SendDataResponse, Task<SendDataResponse>> entry;
            if(currentRequestsList_.TryGetValue(msg.RequestId, out entry) == true)
            {
                if(msg.Type == MessageType.RESPONSE_SUCCESS)
                {
                    entry.Item1.Update(session, msg.Payload, msg.RequestId, true);
                    entry.Item2.Start();
                }
                else if (msg.Type == MessageType.RESPONSE_ERROR)
                {
                    entry.Item1.Update(session, msg.Payload, msg.RequestId, false);
                    entry.Item2.Start();

                }
                currentRequestsList_.Remove(msg.RequestId);
            }
        }

        /// <summary>
        /// CLose connection.
        /// </summary>
        /// <param name="session"></param>
        public void OnConnectionClosed(ISession session)
        {
        }
        #endregion

        #region Private Data Members
        private readonly string VERSION = "1.0";
        private readonly string APPNAME = "NET Client Library";

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, Tuple<SendDataResponse, Task<SendDataResponse>>> currentRequestsList_ = new Dictionary<string, Tuple<SendDataResponse, Task<SendDataResponse>>>();

        private HandleData _messageCallbackHandler = null;

        #endregion
    }
}
