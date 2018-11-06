using System.Threading.Tasks;

namespace Middleware
{
    /// <summary>
    /// Authentication handler interface. authenticates a user, returns true for
    /// pass, false for fail. simple..
    /// </summary>
    interface IAuthenticationHandler
    {
        Task<AuthResult> HandleClientAuthentication(LoginPayload login, string sourceId );
        void AddHandler(IAuthenticationHandler handler);
        void EndpointClosed(string id);
    }

    /// <summary>
    /// Authentication handler base class implements chaining of handlers. will
    /// itertate through all handlers until pass.
    /// </summary>
    abstract class AuthenticationHandler : IAuthenticationHandler
    {
        private IAuthenticationHandler _Next;

        protected abstract Task<AuthResult> AuthenticateUser(LoginPayload login, string sourceId);

        public async Task<AuthResult> HandleClientAuthentication(LoginPayload login, string sourceId)
        {
            var result = await AuthenticateUser(login, sourceId);
            if (result.Result == AuthResult.ResultType.FAILED)
            {
                if (_Next != null)
                {
                    result = await _Next.HandleClientAuthentication(login, sourceId);
                }
            }
            return result;
        }

        public void AddHandler(IAuthenticationHandler handler)
        {
            _Next = handler;
        }

        //inform all authhandlers that this endpoint is closing
        public void EndpointClosed(string id)
        {
            NotifyEndpointClosed(id);
            if(_Next != null)
            {
                _Next.EndpointClosed(id);
            }
        }

        protected virtual void NotifyEndpointClosed(string id) { }
    }
    /// <summary>
    /// default authetication handler hardcoded user and password.
    /// </summary>
    class DefaultAuthenticationHandler : AuthenticationHandler
    {
        protected override Task<AuthResult> AuthenticateUser(LoginPayload login, string sourceId)
        {
            return Task.Factory.StartNew(() =>
           {
               var result = new AuthResult();
               if(login.UserName == "admin" && login.Password == "password")
               {
                   result.Result = AuthResult.ResultType.SUCCESS;
                   result.Message = "Authentication Passed";
               }
               else
               {
                   result.Result = AuthResult.ResultType.FAILED;
                   result.Message = "Authentication Failed";
               }
               return result;
           });
        }
    }   
}
