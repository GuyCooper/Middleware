using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleware
{
    class ConfigParser
    {
        public delegate void Transform(string val);

        struct ParamHolder
        {
            public Transform TransformFunc { get; set; }
            public string Description { get; set; }
            public string Value { get; set; }
        }

        private Dictionary<string, ParamHolder> paramLookup = new Dictionary<string, ParamHolder>();

        public void AddParameter(string key, string description, string defaultVal, Transform transform)
        {
            //first set the default value
            transform(defaultVal);
            paramLookup.Add(key, new ParamHolder { Value = defaultVal, Description = description, TransformFunc = transform });
        }

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

        public void LogValues()
        {
            foreach(var parameter in paramLookup.Values)
            {
                Console.WriteLine("{0}: {1}", parameter.Description, parameter.Value);
            }
        }
    }
}
