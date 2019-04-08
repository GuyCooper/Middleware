using System;
using Newtonsoft.Json;
using NLog;
using MiddlewareInterfaces;

namespace Middleware
{
    /// <summary>
    /// handles login responses from the authentication server client. 
    /// </summary>
    internal class AuthLoginResponseHandler : CommandHandler
    {
        private AuthRequestCache _authCache;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public AuthLoginResponseHandler(AuthRequestCache authCache) : base(HandlerNames.LOGIN)
        {
            _authCache = authCache;
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            Message msg = message.Payload;
            AuthResult authResult = msg.Payload != null ? MiddlewareUtils.DeserialiseObject<AuthResult>(msg.Payload) : null;
            if(_authCache.UpdateAuthResult(msg.RequestId, authResult) == false)
            {
                logger.Log(LogLevel.Error, $"authentication failed. unknown login response message. request id: {msg.RequestId}");
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

        private static Logger logger = LogManager.GetCurrentClassLogger();

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
                logger.Log(LogLevel.Error, "HandleMessageInternal: invalid endpoint");
            }
        }

        public override void RemoveEndpoint(string id)
        {
        }
    }
}
