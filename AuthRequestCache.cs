using System.Collections.Generic;
using System.Threading;
using NLog;
using AuthResultEventPair = System.Tuple<System.Threading.ManualResetEvent, Middleware.AuthResult>;

namespace Middleware
{
    /// <summary>
    /// cache stores all pending auth requests to auth server client
    /// </summary>
    class AuthRequestCache
    {
        private Dictionary<string, AuthResultEventPair> _pendingAuthRequests;
        private int _authTimeout;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public AuthRequestCache(int timeout)
        {
            _pendingAuthRequests = new Dictionary<string, AuthResultEventPair>();
            _authTimeout = timeout;
        }

        public void AddNewAuthRequest(string requestId)
        {
            var authEventPair = new AuthResultEventPair(new ManualResetEvent(false), new AuthResult());
            _pendingAuthRequests.Add(requestId, authEventPair);
        }

        public AuthResult WaitForAuthResult(string requestId)
        {
            AuthResultEventPair authEvent;
            string error = "";
            if (_pendingAuthRequests.TryGetValue(requestId, out authEvent) == false)
            {
                error = "invalid auth request id";
                logger.Log(LogLevel.Error, $"authentication failed: {error}");
            }
            else
            {             
                var result = authEvent.Item1.WaitOne(_authTimeout);
                if (result == true)
                {
                    return authEvent.Item2;
                }

                error = "Timed out waiting for response";
                logger.Log(LogLevel.Error, $"authentication failed: {error}");

                //timed out waiting for response
            }

            return new AuthResult
            {
                Success = false,
                Message = error
            };
        }

        public bool UpdateAuthResult(string requestId, AuthResult result)
        {
            AuthResultEventPair authPair;
            if (_pendingAuthRequests.TryGetValue(requestId, out authPair) == true)
            {
                if (result != null)
                {
                    authPair.Item2.Success = result.Success;
                    authPair.Item2.Message = result.Message;
                }
                else
                {
                    logger.Log(LogLevel.Error, $"authentication failed. invalid authresult for request id {requestId}");
                }
                authPair.Item1.Set(); //signal waiting threads to proceed
                return true;
            }
            else
            {
                logger.Log(LogLevel.Error, $"authentication failed. unable to lookup response key for {requestId}");
            }
            return false;
        }
    }
}
