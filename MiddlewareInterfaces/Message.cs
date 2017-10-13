﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Middleware
{
    public static class MessageHeaders
    {
        public static readonly string CLIENTLOCATION = "ClientLocation";
    }

    public static class HandlerNames
    {
        public static readonly string SUBSCRIBETOCHANNEL = "SUBSCRIBETOCHANNEL";
        public static readonly string REMOVESUBSCRIPTION = "REMOVESUBSCRIPTION";
        public static readonly string SENDMESSAGE = "SENDMESSAGE";
        public static readonly string ADDLISTENER = "ADDLISTENER";
        public static readonly string SENDREQUEST = "SENDREQUEST";
        public static readonly string PUBLISHMESSAGE = "PUBLISHMESSAGE";
    }

    public enum MessageType
    {
        REQUEST = 0,
        UPDATE = 1,
        RESPONSE_ERROR = 2,
        RESPONSE_SUCCESS = 3
    }

    public class Message
    {
        public MessageType Type { get; set; }
        public string RequestId { get; set; }
        public string Command { get; set; }
        public string Channel { get; set; }
        [JsonIgnore]
        public IEndpoint Source { get; set; } //internal use only
        public string SourceId { get; set; }
        public string DestinationId { get; set; }
        public string Payload { get; set; }
    }
}
