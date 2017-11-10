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

        protected override Task<bool> AuthenticateUser(string username, string password)
        {
           return Task.Factory.StartNew(() =>
           {
               return username == TestUser && password == TestPassword;
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
            var todo = UoT.HandleClientAuthentication("admin", "password");
            todo.ContinueWith((result) =>
           {
               Assert.IsTrue(result.Result);
           });
        }

        [TestMethod]
        public void When_using_default_authentication_fail()
        {
            var UoT = new DefaultAuthenticationHandler();
            var todo = UoT.HandleClientAuthentication("admin", "baddy");
            todo.ContinueWith((result) =>
            {
                Assert.IsTrue(result.Result);
            });

        }

        [TestMethod]
        public void When_using_test_authentication_success()
        {
            var UoT = new DefaultAuthenticationHandler();
            UoT.AddHandler(new TestAuthenticationHandler());
            var todo = UoT.HandleClientAuthentication(TestAuthenticationHandler.TestUser,
                                                        TestAuthenticationHandler.TestPassword);
            todo.ContinueWith((result) =>
            {
                Assert.IsTrue(result.Result);
            });

        }

        [TestMethod]
        public void When_using_test_authentication_fail()
        {
            var UoT = new DefaultAuthenticationHandler();
            UoT.AddHandler(new TestAuthenticationHandler());
            var todo = UoT.HandleClientAuthentication("bah", "humbug");
            todo.ContinueWith((result) =>
            {
                Assert.IsTrue(result.Result);
            });
        }
    }
}
