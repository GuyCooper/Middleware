using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Middleware
{
    /// <summary>
    /// interface defines a channel on which to send data
    /// </summary>
    internal interface IConnection
    {
        Task SendData(string data);
    }

    internal class AuthResponse
    {
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

        public AuthResult Result { get; private set; }
        public LoginPayload Payload { get; set; }
    }
    /// <summary>
    /// interface defines an internal representation of the Message class 
    /// </summary>
    internal class MiddlewareMessage
    {
        public MiddlewareMessage(Message payload, IEndpoint source)
        {
            Payload = payload;
            Source = source;
        }

        public Message Payload { get; private set; }
        public IEndpoint Source { get; private set; }
    }

    /// <summary>
    /// interfce defines an endpoint. stores a client connection as well as client
    /// id and authntication information about the client
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
    }

    /// <summary>
    /// middleware specific implmentation of an endpoint class
    /// </summary>
    class MiddlewareEndpoint : IEndpoint
    {
        private IConnection _connection; //underlying socket transport
        private IMessageHandler _handler;
        private IAuthenticationHandler _authHandler;

        public string Id { get; private set; }

        public bool Authenticated { get; private set; }

        public MiddlewareEndpoint(IConnection connection, IMessageHandler handler, IAuthenticationHandler authHandler)
        {
            Id = Guid.NewGuid().ToString();
            _connection = connection;
            _handler = handler;
            _authHandler = authHandler;
        }

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

            var result = await _authHandler.HandleClientAuthentication(login);
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

        public void DataReceived(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            Console.WriteLine("data received on endpoint {0}, {1}", Id, data);

            Message message = JsonConvert.DeserializeObject<Message>(data);
            //populate the session id
            //message.Source = this;
            message.SourceId = Id;

            var internalMessage = new MiddlewareMessage(message, this);

            if (Authenticated == false)
            {
                Console.WriteLine("cannot process requests when endpoint not authenticated");
                throw new InvalidOperationException("endpoint not authenticated");
            }

            //now forward message onto handlers
            if (_handler.ProcessMessage(internalMessage) == false)
            {
                string error = "invalid command. " + message.Command; ;
                Console.WriteLine(error);
                OnError(message, error);
            }
        }

        public void SendData(Message message)
        {
            //ensure that the Source endpoint member is NT serialised
            var payload = JsonConvert.SerializeObject(message);
            _connection.SendData(payload);
        }

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
                _connection.SendData(payload);
            }
        }

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
                    RequestId = message.RequestId
                };
                var payload = JsonConvert.SerializeObject(response);

                _connection.SendData(payload);
            }
        }

        public void EndpointClosed()
        {
            _handler.RemoveEndpoint(Id);
        }
    }
}
