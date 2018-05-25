using System;
using System.Collections.Generic;
using NLog;

namespace Middleware
{
    /// <summary>
    /// ConfigParser. Class parses the command line and processes each paramter with a predefined delegate
    /// </summary>
    class ConfigParser
    {
        #region Internal Types
        public delegate void Transform(string val);

        /// <summary>
        /// ParamHolder struct. Holds the values for each paraeter
        /// </summary>
        struct ParamHolder
        {
            public Transform TransformFunc { get; set; }
            public string Description { get; set; }
            public string Value { get; set; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add a parameter placeholder that defines what to do with the parameter and an optional default value
        /// </summary>
        public void AddParameter(string key, string description, string defaultVal, Transform transform)
        {
            //first set the default value
            transform(defaultVal);
            paramLookup.Add(key, new ParamHolder { Value = defaultVal, Description = description, TransformFunc = transform });
        }

        /// <summary>
        /// Parse the command line and process each command line parameter as defined in AddParameter
        /// </summary>
        public void ParseCommandLine(string[] args)
        {
            foreach(var item in args)
            {
                var index = item.IndexOf(":");
                if(index > 0)
                {
                    var key = item.Substring(0, index);
                    var val = item.Substring(index + 1);
                    ParamHolder param;

                    var lookupVal = key.TrimStart('-').ToUpper();
                    if (paramLookup.TryGetValue(lookupVal, out param) == true)
                    {
                        param.TransformFunc(val);
                        param.Value = val;
                    }
                }
            }
        }

        /// <summary>
        /// Log the values parsed from the command line
        /// </summary>
        public void LogValues()
        {
            foreach(var parameter in paramLookup.Values)
            {
                logger.Log(LogLevel.Info, $"{parameter.Description}:{parameter.Value}");
            }
        }

        #endregion

        #region Private Data Members
        //lookup of parameter name to ParamHolder
        private readonly Dictionary<string, ParamHolder> paramLookup = new Dictionary<string, ParamHolder>();

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();
        #endregion
    }
}
