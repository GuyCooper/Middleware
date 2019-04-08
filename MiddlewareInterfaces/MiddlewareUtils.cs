using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace MiddlewareInterfaces
{
    /// <summary>
    /// Set of various helper methods
    /// </summary>
    public static class MiddlewareUtils
    {
        /// <summary>
        /// Serialise an object to an array.
        /// </summary>
        public static byte[] SerialiseObject(object obj)
        {
            using (var stream = new MemoryStream())
            {
                SerialiseToStream(stream, obj);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Serialise an object to a stream
        /// </summary>
        public static void SerialiseToStream(Stream stream, object obj)
        {
            using (var sw = new StreamWriter(stream))
            {
                using (var writer = new JsonTextWriter(sw))
                {
                    var serialiser = new JsonSerializer();
                    serialiser.Serialize(writer, obj);
                }
            }
        }

        /// <summary>
        /// Serialise an object to a JSON string
        /// </summary>
        public static string SerialiseObjectToString(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Deserialise an object from a json array.
        /// </summary>
        public static T DeserialiseObject<T>( byte[] arr)
        {
            if(arr == null)
            {
                return default(T);
            }

            using (var stream = new MemoryStream(arr))
            {
                using (var sr = new StreamReader(stream))
                {
                    var serialiser = JsonSerializer.Create();
                    return (T)serialiser.Deserialize(sr, typeof(T));
                }
            }
        }

        /// <summary>
        /// Deserialise an object from a Json string.
        /// </summary>
        public static T DeserialiseObject<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }

        /// <summary>
        /// Deserialise the object and retun it as a displayable string.
        /// </summary>
        /// <returns></returns>
        public static string dumpMessageContents(byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                var message = DeserialiseObject<Middleware.Message>(data);
                return $"channel: {message.Channel}\ncommand: {message.Command}\ntype: {message.Type}\nrequestid: {message.RequestId}\nsourceid: {message.SourceId}\ndestinationid: {message.DestinationId}\npayload: {message.Payload}";
            }
            return "";
        }
    }
}
