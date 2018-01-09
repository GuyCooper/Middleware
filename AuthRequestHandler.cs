using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Middleware
{
    /// <summary>
    /// handles login responses from the authentication server client. 
    /// </summary>
    internal class AuthLoginResponseHandler : CommandHandler
    {
        private AuthRequestCache _authCache;

        public AuthLoginResponseHandler(AuthRequestCache authCache) : base(HandlerNames.LOGIN)
        {
            _authCache = authCache;
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            Message msg = message.Payload;
            AuthResult authResult = msg.Payload != null ? JsonConvert.DeserializeObject<AuthResult>(msg.Payload) : null;
            if(_authCache.UpdateAuthResult(msg.RequestId, authResult) == false)
            {
                Console.WriteLine("unknown login response message. request id: {0}", msg.RequestId);
            }
        }

        public override void RemoveEndpoint(string id)
        {
        }
    }

    /// <summary>
    /// class processes authentication server client registration requests
    /// </summary>
    internal class AuthRegisterMessageHandler : CommandHandler
    {
        private IAuthenticationHandler _authHandler;
        private AuthRequestCache _authCache;

        public AuthRegisterMessageHandler(IAuthenticationHandler authHandler, AuthRequestCache authCache) : base(HandlerNames.REGISTER_AUTH)
        {
            _authHandler = authHandler;
            _authCache = authCache;
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            var endpoint = message.Source;
            if (endpoint != null)
            {
                var newHandler = new ExternalAuthenticationHandler(endpoint, _authCache);
                _authHandler.AddHandler(newHandler);
                endpoint.OnSucess(message.Payload);
            }
            else
            {
                Console.WriteLine("invalid endpoint");
            }
        }

        public override void RemoveEndpoint(string id)
        {
        }
    }
}
