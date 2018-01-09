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
            cache.AddNewAuthRequest(requestid);
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public void WhenWaitingForInvalidAuthRequest()
        {
            string requestid = "1234";
            AuthRequestCache cache = new AuthRequestCache(100); //wait for 100 ms
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public void WhenUpdatingValidAuthResult()
        {
            string requestid = "1234";
            AuthRequestCache cache = new AuthRequestCache(100); //wait for 100 ms
            cache.AddNewAuthRequest(requestid);
            AuthResult template = new AuthResult { Success = true, Message = "ok" };
            cache.UpdateAuthResult(requestid, template);
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(result.Message, "ok");
        }

        [TestMethod]
        public void WhenUpdatingInValidAuthResult()
        {
            string requestid = "1234";
            AuthRequestCache cache = new AuthRequestCache(100); //wait for 100 ms
            cache.AddNewAuthRequest(requestid);
            AuthResult template = new AuthResult { Success = false, Message = "bum" };
            var ret = cache.UpdateAuthResult(requestid, template);
            Assert.IsTrue(ret);
            var result = cache.WaitForAuthResult(requestid);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Message, "bum");
        }

    }
}
