using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using NLog;

namespace Middleware
{
    /// <cons>
    /// Interface defines a channel on which to send data.
    /// </summary>
    internal interface IConnection
    {
        /// <summary>
        /// Send data to this connection
        /// </summary>
        void SendData(string data);
    }

    /// <summary>
    /// Class AuthResponse. Defines a response from an Authentication request
    /// </summary>
    internal class AuthResponse
    {
        #region Public Methods

        public AuthResponse(AuthResult result, LoginPayload payload)
        {
            Result = result;
            Payload = payload;
        }

        public AuthResponse(bool success, string message, LoginPayload payload)
        {
            Result = new AuthResult
            {
                Success = success,
                Message = message
            };

            Payload = payload;
        }

        #endregion

        #region Public Properties

        public AuthResult Result { get; private set; }
        public LoginPayload Payload { get; set; }

        #endregion
    }
    /// <summary>
    /// Interface defines an internal representation of the Message class.
    /// </summary>
    internal class MiddlewareMessage
    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public MiddlewareMessage(Message payload, IEndpoint source)
        {
            Payload = payload;
            Source = source;
        }

        #endregion

        #region Public Properties

        //Message payload
        public Message Payload { get; private set; }

        //Source of the message
        public IEndpoint Source { get; private set; }

        #endregion
    }

    /// <summary>
    /// Interface defines an endpoint. stores a client connection as well as client
    /// id and authentication information about the client.
    /// </summary>
    internal interface IEndpoint
    {
        string Id { get; }
        void SendData(Message message);
        void OnError(Message message, string error);
        void OnSucess(Message message);
        void DataReceived(string data);
        bool Authenticated { get; }
        Task<AuthResponse> AuthenticateEndpoint(string data);
        void EndpointClosed();
        void NotifySessionClosed(string id);
    }

    /// <summary>
    /// Middleware specific implmentation of an endpoint class.
    /// </summary>
    class MiddlewareEndpoint : IEndpoint
    {
        #region Public Properties
        // ID of endpoint
        public string Id { get; private set; }

        // true id endpoint has been authenticated otherwise false
        public bool Authenticated { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public MiddlewareEndpoint(IConnection connection, IMessageHandler handler, IAuthenticationHandler authHandler)
        {
            Id = Guid.NewGuid().ToString();
            _connection = connection;
            _handler = handler;
            _authHandler = authHandler;
        }

        /// <summary>
        /// Authenticate this endpoint with a loginrequest.
        /// </summary>
        public async Task<AuthResponse> AuthenticateEndpoint(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new AuthResponse(false, "invalid payload", null);
            }

            Message message;
            try
            {
                message = JsonConvert.DeserializeObject<Message>(data);
                if (message.Command != HandlerNames.LOGIN)
                {
                    return new AuthResponse(false, "Cannot process request. User not authenticated", null);
                }
            }
            catch (Exception e)
            {
                return new AuthResponse(false, "cannot deserailise login message. " + e.Message, null);
            }

            LoginPayload login = JsonConvert.DeserializeObject<LoginPayload>(message.Payload);

            var result = await _authHandler.HandleClientAuthentication(login, Id);
            Authenticated = result.Success;
            message.Payload = result.Message;
            if (Authenticated == true)
            {
                OnSucess(message);
            }
            else
            {
                OnError(message, "authentication failed");
            }

            return new AuthResponse(result, login);
        }

        /// <summary>
        /// Method called when data received on this endpoint.
        /// </summary>
        public void DataReceived(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            logger.Log(LogLevel.Trace, $"data received on endpoint {Id},thread id {Thread.CurrentThread.ManagedThreadId}. data {data}");

            Message message = JsonConvert.DeserializeObject<Message>(data);
            //populate the session id
            //message.Source = this;
            message.SourceId = Id;

            var internalMessage = new MiddlewareMessage(message, this);

            if (Authenticated == false)
            {
                logger.Log(LogLevel.Error, "Endpoint: DataReceived. Endpoint not authenticated.");
                throw new InvalidOperationException("Endpoint not authenticated");
            }

            //now forward message onto handlers
            if (_handler.ProcessMessage(internalMessage) == false)
            {
                string error = "Invalid command. " + message.Command;
                logger.Log(LogLevel.Error, error);
                OnError(message, error);
            }
        }

        /// <summary>
        /// Send data on this endpoint
        /// </summary>
        public void SendData(Message message)
        {
            //ensure that the Source endpoint member is NT serialised
            var payload = JsonConvert.SerializeObject(message);
            _connection.SendData(payload);
        }

        /// <summary>
        /// Send an error message response on this endpoint.
        /// </summary>
        public void OnError(Message message, string error)
        {
            //do not send responses back to client for update message types
            //(data publish or responses)
            if (message.Type == MessageType.REQUEST)
            {
                //Send error response to client
                var response = new Message
                {
                    Type = MessageType.RESPONSE_ERROR,
                    RequestId = message.RequestId,
                    Payload = error
                };

                var payload = JsonConvert.SerializeObject(response);

                logger.Log(LogLevel.Trace, $"Endpoint OnError. {payload}.");
                _connection.SendData(payload);
            }
        }

        /// <summary>
        /// Send a success response on this endpoint
        /// </summary>
        public void OnSucess(Message message)
        {
            //do not send responses back to client for update message types
            //(data publish or responses)
            if (message.Type == MessageType.REQUEST)
            {
                //send a success response back to the client
                var response = new Message
                {
                    Type = MessageType.RESPONSE_SUCCESS,
                    RequestId = message.RequestId,
                    Channel = message.Channel,
                    Command = message.Command
                };
                var payload = JsonConvert.SerializeObject(response);

                logger.Log(LogLevel.Trace, $"Endpoint OnSuccess. {payload}");
                _connection.SendData(payload);
            }
        }

        /// <summary>
        /// Close the endpoint.
        /// </summary>
        public void EndpointClosed()
        {
            _handler.RemoveEndpoint(Id);
            //inform auth handler that this session is closing
            _authHandler.EndpointClosed(Id);
        }

        /// <summary>
        /// Notify this endpont that this session id has just closed.
        /// </summary>
        public void NotifySessionClosed(string id)
        {
            logger.Log(LogLevel.Trace, $"Endpoint NotifySessionClosed. Id: {id}");
            //previous session id stored in case this method is called multiple times 
            //on the same session
            if (_previousSessionClose != id)
            {
                _previousSessionClose = id;
                var message = new Message
                {
                    Command = HandlerNames.NOTIFY_CLOSE,
                    Payload = id,
                    Type = MessageType.UPDATE
                };
                SendData(message);
            }
        }

        #endregion

        #region Private Data Members

        //Connection to underlying transport
        private IConnection _connection; 

        //Object to handle all messages on this endpoint.
        private IMessageHandler _handler;

        // Authentication handler for this endpoint
        private IAuthenticationHandler _authHandler;

        //previous session id on this endpoint
        private string _previousSessionClose;

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }
}
