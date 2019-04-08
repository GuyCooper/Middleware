using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Middleware
{
    /// <summary>
    /// Serialisable xml class for cxonnection stats
    /// </summary>
    [XmlType("connection")]
    public class Connection
    {
        [XmlElement("id")]
        public string Id { get; set; }
        [XmlElement("remote-address")]
        public string Address { get; set; }
        [XmlElement("app-name")]
        public string AppName { get; set; }
        [XmlElement("version")]
        public string Version { get; set; }
        [XmlElement("requests")]
        public int Requests { get; set; }
        [XmlElement("data")]
        public double DataUpdates { get; set; }
    }

    /// <summary>
    /// Serialisable class for channel stats
    /// </summary>
    [XmlType("channel")]
    public class ChannelStats
    {
        [XmlElement("name")]
        public string ChannelName { get; set; }
        [XmlElement("requests")]
        public int Requests { get; set; }
        [XmlElement("data")]
        public double DataUpdates { get; set; }
    }

    /// <summary>
    /// Serialisable class for stats
    /// </summary>
    [XmlRoot(ElementName = "stats")]
    public class FullStats
    {
        [XmlArray("channels")]
        public ChannelStats[] Channels { get; set; }
        [XmlElement("licensed-to")]
        public string LicensedTo { get; set; }
        [XmlElement("max-connections")]
        public int MaxConnections { get; set; }
        [XmlElement("current-connections")]
        public int CurrentConnections { get; set; }
        [XmlArray("connections")]
        public Connection[] Connections { get; set; }
    }

    /// <summary>
    /// Interface for middleware stats
    /// </summary>
    interface IMessageStats
    {
        void UpdateChannelStats(Message message);
        void NewConnection(string id, string source, string appName, string version, bool isAuth);
        void CloseConnection(string id, bool isAuth);
        IEnumerable<ChannelStats> GetChannelsStats();
        string ToXML();
    }

    /// <summary>
    /// Message stats class. Stores message stats in a cache and serialises them out into an xml format.
    /// </summary>
    class MessageStats : IMessageStats
    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public MessageStats(string licenedTo, int maxConnections)
        {
            _licenedTo = licenedTo;
            _maxConnections = maxConnections;
            _currentConnections = 0;
        }

        /// <summary>
        /// Update the channel stats
        /// </summary>
        public void UpdateChannelStats(Message message)
        {
            ChannelStats channeldata;
            if(_channelStats.TryGetValue(message.Channel, out channeldata) == false)
            {
                channeldata = new ChannelStats();
                channeldata.ChannelName = message.Channel;
                _channelStats.Add(message.Channel, channeldata);
            }

            var connection = _connections.Find(x => x.Id == message.SourceId);

            if(message.Type == MessageType.REQUEST)
            {
                channeldata.Requests++;
                if(connection != null)
                {
                    connection.Requests++;
                }
            }
            else if(message.Type == MessageType.UPDATE)
            {
                if(message.Payload != null)
                {
                    channeldata.DataUpdates += message.Payload.Length;
                    if(connection != null)
                    {
                        connection.DataUpdates += message.Payload.Length;
                    }
                }
            }
        }

        /// <summary>
        /// New connection received on middleware service
        /// </summary>
        public void NewConnection(string id, string source, string appName, string version, bool isAuth)
        {
            if (_connections.Find(x => x.Id == id) == null)
            {
                _currentConnections++;
                _connections.Add(new Connection
                {
                    Id = id,
                    Address = source,
                    AppName = appName,
                    Version = version
                });
            }
        }

        /// <summary>
        /// Connection closed on middleware service.
        /// </summary>
        public void CloseConnection(string id, bool isAuth)
        {
            _currentConnections--;
            var connection = _connections.Find(x => x.Id == id);
            if(connection != null)
            {
                _connections.Remove(connection);
            }
        }

        /// <summary>
        /// return the cache of channel stats
        /// </summary>
        public IEnumerable<ChannelStats> GetChannelsStats()
        {
            return _channelStats.Values;
        }

        /// <summary>
        /// return an xml serialised  string of all the stats.
        /// </summary>
        public string ToXML()
        {
            var writer = new StringWriter();
            XmlSerializer serialiser = new XmlSerializer(typeof(FullStats));
            var xmlData = new FullStats
            {
                Channels = GetChannelsStats().ToArray(),
                LicensedTo = _licenedTo,
                MaxConnections = _maxConnections,
                Connections = _connections.ToArray(),
                CurrentConnections = _currentConnections
            };

            using (XmlWriter xmlout = XmlWriter.Create(writer))
            {
                xmlout.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"index.xslt\"");
                serialiser.Serialize(xmlout, xmlData);
            }
            //serialiser.Serialize(writer, xmlData);
            return writer.ToString();
        }

        #endregion

        #region Private Data Members

        private Dictionary<string, ChannelStats> _channelStats = new Dictionary<string, ChannelStats>();

        private string _licenedTo;
        private int _maxConnections;
        private int _currentConnections;

        private List<Connection> _connections = new List<Connection>();

        #endregion
    }
}
