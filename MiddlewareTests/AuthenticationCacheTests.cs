using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Middleware;

namespace MiddlewareTests
{
    [TestClass]
    public class AuthenticationCacheTests
    {
        [TestMethod]
        public void WhenWaitingForAuthRequestTimeout()
        {
            string requestid = "1234";
            AuthRequestCache cache = new AuthRequestCache(100); //wait for 100 ms
            cache.AddNewAuthRequest(null,requestid);
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.AreEqual(AuthResult.ResultType.FAILED, result.Result);
        }

        [TestMethod]
        public void WhenWaitingForInvalidAuthRequest()
        {
            string requestid = "1234";
            AuthRequestCache cache = new AuthRequestCache(100); //wait for 100 ms
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.AreEqual(AuthResult.ResultType.FAILED, result.Result);
        }

        [TestMethod]
        public void WhenUpdatingValidAuthResult()
        {
            string sourceid = "abc";
            string requestid = "1234";
            AuthRequestCache cache = new AuthRequestCache(100); //wait for 100 ms
            cache.AddNewAuthRequest(sourceid, requestid);
            AuthResult template = new AuthResult { Result = AuthResult.ResultType.SUCCESS, Message = "ok" };
            cache.UpdateAuthResult(requestid, template);
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.AreEqual(AuthResult.ResultType.SUCCESS, result.Result);
            Assert.AreEqual(result.Message, "ok");
            Assert.AreEqual(sourceid, result.ConnectionId);
        }

        [TestMethod]
        public void WhenUpdatingInValidAuthResult()
        {
            string sourceid = "abc";
            string requestid = "1234";
            AuthRequestCache cache = new AuthRequestCache(100); //wait for 100 ms
            cache.AddNewAuthRequest(sourceid, requestid);
            AuthResult template = new AuthResult { Result = AuthResult.ResultType.FAILED, Message = "bum" };
            var ret = cache.UpdateAuthResult(requestid, template);
            Assert.IsTrue(ret);
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.AreEqual(AuthResult.ResultType.FAILED, result.Result);
            Assert.AreEqual(result.Message, "bum");
            Assert.AreEqual(0, result.ConnectionId);
        }
    }
}
