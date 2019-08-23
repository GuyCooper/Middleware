using NLog;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Middleware
{
    /// <summary>
    /// XML serialisible Class contains configuration parameters for middleware process.
    /// </summary>
    [XmlRoot(ElementName = "configuration")]
    public class ConfigurationSettings
    {
        #region Public Properties

        [XmlElement("hosturls")]
        public string URLS { get; set; }
        [XmlElement("authurls")]
        public string AUTHURLS { get; set; }
        [XmlElement("maxconnections")]
        public int MaxConnections { get; set; }
        [XmlElement("maxauthconnections")]
        public int MaxAuthConnections { get; set; }
        [XmlElement("authtimeoutms")]
        public int AuthTimeoutMS { get; set; }
        [XmlElement("filehandlerpath")]
        public string FileRequestHandlerPath { get; set; }

        #endregion

    }

    /// <summary>
    /// MiddlewareConfig class. Parses the configuration file.
    /// </summary>
    class MiddlewareConfig
    {
        #region Constructor

        /// <summary>
        /// Constructor. Parse the config file
        /// </summary>
        public MiddlewareConfig(string filename, Logger logger, string certificate)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new FileNotFoundException("Must specify a valid configuration file.");
            }

            if (Path.GetExtension(filename) == ".enc")
            {
                //Configuration file is encoded.
                var decrypted = DataEncryption.Encryption.DecryptData(File.ReadAllBytes(filename), certificate);
                using (var ms = new MemoryStream(decrypted))
                {
                    LoadFromStream(ms);
                }
                //File.Delete(tmpFile);
            }
            else
            {
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    LoadFromStream(fs);
                }
            }

            //log the parameters
            var properties = m_configuration.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                logger.Info($"{property.Name} : {property.GetValue(m_configuration)}");
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Load the configuration from an xml stream.
        /// </summary>
        private void LoadFromStream(Stream stream)
        {
            XmlSerializer serialiser = new XmlSerializer(typeof(ConfigurationSettings));
            m_configuration = (ConfigurationSettings)serialiser.Deserialize(stream);
        }

        #endregion

        /// <summary>
        /// Constructor. Parse the command line parameters
        /// </summary>
        //public MiddlewareConfig(string[] args)
        //{
        //    ConfigParser parser = new ConfigParser();
        //    parser.AddParameter("H", "server url", "http://localhost:8080/MWARE/", (val) => { this.URLS = val; });
        //    //parser.AddParameter("H", "server url","https://localhost:8443/", (val) => { this.URLS = val; });
        //    parser.AddParameter("S", "auth url", "http://localhost:9092/", (val) => { this.AUTHURLS = val; });
        //    parser.AddParameter("M", "max client connections", "10", (val) => { this.MaxConnections = int.Parse(val); });
        //    parser.AddParameter("N", "max auth client connections", "1", (val) => { this.MaxAuthConnections = int.Parse(val); });
        //    parser.AddParameter("R", "root folder", @"C:\Projects\Middleware\Middleware", (val) => { this.RootFolder = val; });
        //    parser.AddParameter("T", "authg response timeout", "30000", (val) => { this.AuthTimeout = int.Parse(val); });

        //    parser.ParseCommandLine(args);
        //    parser.LogValues();
        //}

        #region public properties

        public string URLS { get { return m_configuration.URLS; } }
        public string AUTHURLS { get { return m_configuration.AUTHURLS; } }
        public int MaxConnections { get { return m_configuration.MaxConnections; } }
        public int MaxAuthConnections { get { return m_configuration.MaxAuthConnections; } }
        public int AuthTimeoutMS { get { return m_configuration.AuthTimeoutMS; } }
        public string FileRequestHandlerPath { get { return m_configuration.FileRequestHandlerPath; } }

        #endregion

        #region Private Data

        private ConfigurationSettings m_configuration;

        #endregion
    }
}
