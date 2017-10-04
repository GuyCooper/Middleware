using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Middleware
{
    [XmlType("connection")]
    public class Connection
    {
        [XmlElement("id")]
        public string Id { get; set; }
        [XmlElement("remote-address")]
        public string Address { get; set; }
        [XmlElement("requests")]
        public int Requests { get; set; }
        [XmlElement("data")]
        public double DataUpdates { get; set; }
    }

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

    interface IMessageStats
    {
        void UpdateChannelStats(Message message);
        void OpenConnection(string id, string origin);
        void CloseConnection(string id);
        IEnumerable<ChannelStats> GetChannelsStats();
        string ToXML();
    }

    class MessageStats : IMessageStats
    {
        private Dictionary<string, ChannelStats> _channelStats = new Dictionary<string, ChannelStats>();

        private string _licenedTo;
        private int _maxConnections;
        private int _currentConnections;

        private List<Connection> _connections = new List<Connection>();

        public MessageStats(string licenedTo, int maxConnections)
        {
            _licenedTo = licenedTo;
            _maxConnections = maxConnections;
            _currentConnections = 0;
        }

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
                if(string.IsNullOrEmpty(message.Payload) == false)
                {
                    channeldata.DataUpdates += message.Payload.Length;
                    if(connection != null)
                    {
                        connection.DataUpdates += message.Payload.Length;
                    }
                }
            }
        }

        public void OpenConnection(string id, string origin)
        {
            if (_connections.Find(x => x.Id == id) == null)
            {
                _currentConnections++;
                _connections.Add(new Connection
                {
                    Id = id,
                    Address = origin
                });
            }
        }

        public void CloseConnection(string id)
        {
            _currentConnections--;
            var connection = _connections.Find(x => x.Id == id);
            if(connection != null)
            {
                _connections.Remove(connection);
            }
        }

        public IEnumerable<ChannelStats> GetChannelsStats()
        {
            return _channelStats.Values;
        }

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
    }
}
