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
        Task<bool> AuthenticateEndpoint(string data);
        void EndpointClosed();
    }

    /// <summary>
    /// middleware specific implmentation of an endpoint class
    /// </summary>
    class MiddlewareEndpoint : IEndpoint
    {
        private IConnection _connection; //underlying socket transport
        private IHandler _handler;
        private IAuthenitcationHandler _authHandler;

        public string Id { get; private set; }

        public bool Authenticated { get; private set; }

        public MiddlewareEndpoint(IConnection connection, IHandler handler, IAuthenitcationHandler authHandler)
        {
            Id = Guid.NewGuid().ToString();
            _connection = connection;
            _handler = handler;
            _authHandler = authHandler;
        }

        public async Task<bool> AuthenticateEndpoint(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            Message message;
            try
            {
                message = JsonConvert.DeserializeObject<Message>(data);
                if (message.Command != HandlerNames.LOGIN)
                {
                    Console.WriteLine("Cannot process request. User not authenticated");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("cannot deserailise login message. {0}", e.Message);
                return false;
            }

            LoginPayload login = JsonConvert.DeserializeObject<LoginPayload>(message.Payload);

            var result = await _authHandler.HandleClientAuthentication(login.UserName, login.Password);
            if (result == true)
            {
                message.Payload = "authentication succeded";
                OnSucess(message);
                Authenticated = true;
            }
            else
            {
                OnError(message, "authentication failed");
            }

            return result;
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
