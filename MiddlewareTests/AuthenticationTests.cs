using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;

namespace MiddlewareTests
{
    class TestAuthenticationHandler : AuthenticationHandler
    {
        public static readonly string TestUser = "bob";
        public static readonly string TestPassword = "piddy";

        protected override Task<AuthResult> AuthenticateUser(LoginPayload login)
        {
           return Task.Factory.StartNew(() =>
           {
               return new AuthResult
               {
                   Success = login.UserName == TestUser && login.Password == TestPassword
               };
           });
        }
    }

    [TestClass]
    public class AuthenticationTests
    {
        [TestMethod]
        public void When_using_default_authentication_success()
        {
            var UoT = new DefaultAuthenticationHandler();
            var login = new LoginPayload { UserName = "admin", Password = "password" };
            var todo = UoT.HandleClientAuthentication(login);
            todo.ContinueWith((result) =>
           {
               Assert.IsTrue(result.Result.Success);
           });
        }

        [TestMethod]
        public void When_using_default_authentication_fail()
        {
            var UoT = new DefaultAuthenticationHandler();
            var login = new LoginPayload { UserName = "admin", Password = "baddy" };
            var todo = UoT.HandleClientAuthentication(login);
            todo.ContinueWith((result) =>
            {
                Assert.IsFalse(result.Result.Success);
            });

        }

        [TestMethod]
        public void When_using_test_authentication_success()
        {
            var UoT = new DefaultAuthenticationHandler();
            UoT.AddHandler(new TestAuthenticationHandler());
            var login = new LoginPayload
            {
                UserName = TestAuthenticationHandler.TestUser,
                Password = TestAuthenticationHandler.TestPassword
            };

            var todo = UoT.HandleClientAuthentication(login);
            todo.ContinueWith((result) =>
            {
                Assert.IsTrue(result.Result.Success);
            });

        }

        [TestMethod]
        public void When_using_test_authentication_fail()
        {
            var UoT = new DefaultAuthenticationHandler();
            UoT.AddHandler(new TestAuthenticationHandler());

            var login = new LoginPayload { UserName = "bah", Password = "humbug" };

            var todo = UoT.HandleClientAuthentication(login);
            todo.ContinueWith((result) =>
            {
                Assert.IsFalse(result.Result.Success);
            });
        }
    }
}
