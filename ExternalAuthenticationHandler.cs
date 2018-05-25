using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Middleware
{
    /// <summary>
    /// External Module Authentication handler. Processes authentication requests by passing the request to the
    /// endpoint passed in the constructor. The rquest is cached in the AuthRequestCache so the response can be
    /// mapped to the original request
    /// </summary>
    class ExternalAuthenticationHandler : AuthenticationHandler
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExternalAuthenticationHandler(IEndpoint endpoint, AuthRequestCache authCache)
        {
            _endpoint = endpoint;
            _authCache = authCache;
        }

        /// <summary>
        /// Method makes an asynchronous request to external auth server and waits for response.
        /// </summary>
        protected override Task<AuthResult> AuthenticateUser(LoginPayload login, string sourceId)
        {
            //marshall request onto its own thread so it can then block while waiting for auth response
            return Task.Factory.StartNew(() =>
            {
                var message = new Message
                {
                    Command = HandlerNames.LOGIN,
                    Type = MessageType.REQUEST,
                    Payload = JsonConvert.SerializeObject(login),
                    RequestId = Guid.NewGuid().ToString(),
                    SourceId = sourceId
                };

                //create auth request
                _authCache.AddNewAuthRequest(message.RequestId);
                _endpoint.SendData(message);
                //wait for response from auth server
                return _authCache.WaitForAuthResult(message.RequestId);
            });
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Method called when endpoint using this authneitcation handler has been closed
        /// remotely. Inform the remote authentication service that it needs to close this 
        /// session.
        /// </summary>
        protected override void NotifyEndpointClosed(string id) 
        {
            var message = new Message
            { 
                Command = HandlerNames.NOTIFY_CLOSE,
                Type = MessageType.REQUEST,
                Payload = id,
                RequestId = Guid.NewGuid().ToString()
            };

            _endpoint.SendData(message);
        }

        #endregion

        #region Private Data Members

        // Endpont to remote authentication service
        private IEndpoint _endpoint;

        // Cache that holds all authentiuction requests to remote clients
        private AuthRequestCache _authCache;
        #endregion
    }
}
