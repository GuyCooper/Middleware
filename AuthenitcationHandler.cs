using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Middleware
{
    /// <summary>
    /// Authentication handler interface. authenticates a user, returns true for
    /// pass, false for fail. simple..
    /// </summary>
    interface IAuthenticationHandler
    {
        Task<AuthResult> HandleClientAuthentication(LoginPayload login );
        void AddHandler(IAuthenticationHandler handler);
    }

    /// <summary>
    /// Authrntication handler base class implements chaining of handlers. will
    /// itertate through all handlers until pass.
    /// </summary>
    abstract class AuthenticationHandler : IAuthenticationHandler
    {
        private IAuthenticationHandler _Next;

        protected abstract Task<AuthResult> AuthenticateUser(LoginPayload login);

        public async Task<AuthResult> HandleClientAuthentication(LoginPayload login)
        {
            var result = await AuthenticateUser(login);
            if(result.Success == false)
            {
                if (_Next != null)
                {
                    result = await _Next.HandleClientAuthentication(login);
                }
            }
            return result;
        }

        public void AddHandler(IAuthenticationHandler handler)
        {
            _Next = handler;
        }
    }

    /// <summary>
    /// default authetication handler hardcoded user and password.
    /// </summary>
    class DefaultAuthenticationHandler : AuthenticationHandler
    {
        protected override Task<AuthResult> AuthenticateUser(LoginPayload login)
        {
            return Task.Factory.StartNew(() =>
           {
               var result = new AuthResult();
               if(login.UserName == "admin" && login.Password == "password")
               {
                   result.Success = true;
                   result.Message = "Autheitcation Passed";
               }
               else
               {
                   result.Success = false;
                   result.Message = "Autheitcation Failed";
               }
               return result;
           });
        }
    }   
}
