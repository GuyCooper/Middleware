using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiddlewareInterfaces;

namespace MiddlewareTests
{
    /// <summary>
    /// Serliaser unit tests.
    /// </summary>
    [TestClass]
    public class SerialiserTests
    {
        [TestMethod]
        public void SerialiseStringTest()
        {
            var test = "test";
            var serialised = MiddlewareUtils.SerialiseObject(test);
            var deserialised = MiddlewareUtils.DeserialiseObject<string>(serialised);
            Assert.AreEqual(test, deserialised);
        }

        private class TestClass
        {
            public double Value { get; set; }
        }

        [TestMethod]
        public void SerialiseObjectTest()
        {
            var obj = new TestClass { Value = 32.1432 };
            var serialised = MiddlewareUtils.SerialiseObject(obj);
            var deserialised = MiddlewareUtils.DeserialiseObject<TestClass>(serialised);
            Assert.AreEqual(obj.Value, deserialised.Value);
        }
    }
}
