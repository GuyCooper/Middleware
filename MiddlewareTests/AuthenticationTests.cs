﻿using System;
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

        protected override Task<AuthResult> AuthenticateUser(LoginPayload login, string sourceId)
        {
           return Task.Factory.StartNew(() =>
           {
               return new AuthResult
               {
                   Result = (login.UserName == TestUser && login.Password == TestPassword) ? AuthResult.ResultType.SUCCESS : AuthResult.ResultType.FAILED
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
            var todo = UoT.HandleClientAuthentication(login, "1");
            todo.ContinueWith((result) =>
           {
               Assert.AreEqual(AuthResult.ResultType.SUCCESS, result.Result);
           });
        }

        [TestMethod]
        public void When_using_default_authentication_fail()
        {
            var UoT = new DefaultAuthenticationHandler();
            var login = new LoginPayload { UserName = "admin", Password = "baddy" };
            var todo = UoT.HandleClientAuthentication(login, "1");
            todo.ContinueWith((result) =>
            {
                Assert.AreEqual(AuthResult.ResultType.FAILED, result.Result);
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

            var todo = UoT.HandleClientAuthentication(login, "1");
            todo.ContinueWith((result) =>
            {
                Assert.AreEqual(AuthResult.ResultType.SUCCESS, result.Result);
            });

        }

        [TestMethod]
        public void When_using_test_authentication_fail()
        {
            var UoT = new DefaultAuthenticationHandler();
            UoT.AddHandler(new TestAuthenticationHandler());

            var login = new LoginPayload { UserName = "bah", Password = "humbug" };

            var todo = UoT.HandleClientAuthentication(login, "1");
            todo.ContinueWith((result) =>
            {
                Assert.AreEqual(AuthResult.ResultType.FAILED, result.Result);
            });
        }
    }
}
