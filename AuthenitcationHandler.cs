using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleware
{
    /// <summary>
    /// Authentication handler interface. authenticates a user, returns true for
    /// pass, false for fail. simple..
    /// </summary>
    interface IAuthenitcationHandler
    {
        Task<bool> HandleClientAuthentication(string username, string password);
        void AddHandler(IAuthenitcationHandler handler);
    }

    /// <summary>
    /// Authrntication handler base class implements chaining of handlers. will
    /// itertate through all handlers until pass.
    /// </summary>
    abstract class AuthenticationHandler : IAuthenitcationHandler
    {
        private IAuthenitcationHandler _Next;

        protected abstract Task<bool> AuthenticateUser(string username, string password);

        public async Task<bool> HandleClientAuthentication(string username, string password)
        {
            var result = await AuthenticateUser(username, password);
            if(result == false)
            {
                if (_Next != null)
                {
                    result = await AuthenticateUser(username, password);
                }
            }
            return result;
        }

        public void AddHandler(IAuthenitcationHandler handler)
        {
            _Next = handler;
        }
    }

    /// <summary>
    /// default authetication handler hardcoded user and password.
    /// </summary>
    class DefaultAuthenticationHandler : AuthenticationHandler
    {
        protected override Task<bool> AuthenticateUser(string username, string password)
        {
            return Task.Factory.StartNew(() =>
           {
               return username == "admin" && password == "password";
           });
        }
    }

    /// <summary>
    /// use external authentication module to authenticate
    /// </summary>
    class ExternalAuthenticationHandler : AuthenticationHandler
    {
        protected override Task<bool> AuthenticateUser(string username, string password)
        {
            throw new NotImplementedException();
        }
    }
}
