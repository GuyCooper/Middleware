using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Middleware
{
    /// <summary>
    /// use external authentication module to authenticate
    /// </summary>
    class ExternalAuthenticationHandler : AuthenticationHandler
    {
        private IEndpoint _endpoint;
        private AuthRequestCache _authCache;

        public ExternalAuthenticationHandler(IEndpoint endpoint, AuthRequestCache authCache)
        {
            _endpoint = endpoint;
            _authCache = authCache;
        }

        /// <summary>
        /// method makes an asynchronous request to external auth server and waits for response
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        protected override Task<AuthResult> AuthenticateUser(LoginPayload login)
        {
            return Task.Factory.StartNew(() =>
            {
                var message = new Message
                {
                    Command = HandlerNames.LOGIN,
                    Type = MessageType.REQUEST,
                    Payload = JsonConvert.SerializeObject(login),
                    RequestId = Guid.NewGuid().ToString()
                };

                //create auth request
                _authCache.AddNewAuthRequest(message.RequestId);
                _endpoint.SendData(message);
                //wait for response from auth server
                return _authCache.WaitForAuthResult(message.RequestId);
            });
        }
    }
}
