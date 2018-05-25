using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Middleware
{
    /// <summary>
    /// Static class containing message header keys.
    /// </summary>
    public static class MessageHeaders
    {
        public static readonly string CLIENTLOCATION = "ClientLocation";
        public static readonly string CLIENTUSERNAME = "ClientUserName";
        public static readonly string CLIENTPASSWORD = "ClientPassword";
        public static readonly string AUTHENTICATION_KEY = "Sec-WebSocket-Key";
    }

    /// <summary>
    /// Static class containing the list of commands supported by the middleware service.
    /// </summary>
    public static class HandlerNames
    {
        //add a subscriber to a channel to receive any broadcasts
        public static readonly string SUBSCRIBETOCHANNEL = "SUBSCRIBETOCHANNEL";
        //remove subscriber
        public static readonly string REMOVESUBSCRIPTION = "REMOVESUBSCRIPTION";
        //send a message to a specified recipient
        public static readonly string SENDMESSAGE = "SENDMESSAGE";
        //add a listener to handle all requests on a channel
        public static readonly string ADDLISTENER = "ADDLISTENER";
        //send a request to a channel, must have a listener to be processed
        public static readonly string SENDREQUEST = "SENDREQUEST";
        //broadcast a message to a channel, will be handled by all subscribers
        public static readonly string PUBLISHMESSAGE = "PUBLISHMESSAGE";
        //send a login request to middleware. requested by library on connection.
        public static readonly string LOGIN = "DOLOGIN";
        //register an authentication handler to process login requests
        public static readonly string REGISTER_AUTH = "REGISTER_AUTH";
        //notify a channel controller that a client session has closed
        public static readonly string NOTIFY_CLOSE = "NOTIFY_CLOSE";
    }

    /// <summary>
    /// Enum of message types. requests have a corresponding response that is handled
    /// in the client (either response_error or response_success. Updates are just forwarded to client
    /// </summary>
    public enum MessageType
    {
        REQUEST = 0,
        UPDATE = 1,
        RESPONSE_ERROR = 2,
        RESPONSE_SUCCESS = 3
    }

    /// <summary>
    /// The basic message that is used by the middleware service
    /// </summary>
    public class Message
    {
        public MessageType Type { get; set; }
        public string RequestId { get; set; }
        public string Command { get; set; }
        public string Channel { get; set; }
        public string SourceId { get; set; }
        public string DestinationId { get; set; }
        public string Payload { get; set; }
    }

    public class LoginPayload
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Version { get; set; }
        public string AppName { get; set; }
        public string Source { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
