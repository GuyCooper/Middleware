using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiddlewareInterfaces
{
    /// <summary>
    /// Exception thrown if query params cannot be validated
    /// </summary>
    public class UnauthenticatedUserException : Exception
    {
    }

    /// <summary>
    /// Class defines the parameters from a http query. Includes the query identifier and 
    /// a list of parameters. 
    /// </summary>
    public class QueryParams
    {
        #region Public Properties

        /// <summary>
        /// Query Identifier
        /// </summary>
        public string Identifier { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public QueryParams(HttpListenerRequest request)
        {
            Identifier = Path.GetFileName(request.Url.AbsolutePath);
            var query = DecodeURLString(request.Url.Query);
            var paramIndex = query.IndexOf('?');
            if (paramIndex > -1)
            {
                //This request has parameters
                m_params = query.Substring(paramIndex + 1);
            }
        }

        /// <summary>
        /// Method validates the parameters and returns a serialised json form of the parameters.
        /// Does a bit more than it says on the tin. 
        /// </summary>
        /// <returns></returns>
        public string ValidateParameters(string usertoken, Func<string, bool> validateUser)
        {
            if(string.IsNullOrEmpty(m_params))
            {
                return null;
            }

            var validated = false;

            char badOne = m_params.FirstOrDefault(ch => m_badChars.Contains(ch));
            if (badOne != default(char))
            {
                throw new ArgumentException($"Unsafe characters in request. cannot process for security reasons.");
            }

            var parts = m_params.Split(';');
            var jsonStringBuilder = new StringBuilder("{");
            for (var index = 0; index < parts.Length; index++)
            {
                var element = parts[index].Split('=');
                if (element.Length != 2)
                {
                    throw new ArgumentException("Badly formed query string!");
                }

                var key = element[0];
                var val = element[1];

                if (key.Equals(usertoken, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    validated = validateUser(val);
                }
                else
                {
                    jsonStringBuilder.Append($"\"{key}\" : ");
                    double dVal;
                    if (double.TryParse(element[1], out dVal) == true)
                    {
                        jsonStringBuilder.Append($"{val}");
                    }
                    else
                    {
                        jsonStringBuilder.Append($"\"{val}\"");
                    }

                    if (index < (parts.Length - 1))
                    {
                        jsonStringBuilder.Append(",");
                    }
                }
            }
            jsonStringBuilder.Append("}");

            if(validated == false)
            {
                throw new UnauthenticatedUserException();
            }

            return jsonStringBuilder.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method decodes a url string.
        /// </summary>
        private static string DecodeURLString(string urlString)
        {
            string result = urlString;
            foreach(var decode in m_urlConvert)
            {
                result = result.Replace(decode.Key, decode.Value);
            }
            return result;
        }

        #endregion

        #region Private Data

        private string m_params;

        private static readonly List<char> m_badChars = new List<char>
            {
                '>',
                '<'
            };

        private static List<KeyValuePair<string, string>> m_urlConvert = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("%20"," "),
            new KeyValuePair<string, string>("%21","!"),
            new KeyValuePair<string, string>("%22","\""),
            new KeyValuePair<string, string>("%23","#"),
            new KeyValuePair<string, string>("%24","$"),
            new KeyValuePair<string, string>("%25","%"),
            new KeyValuePair<string, string>("%26","&"),
            new KeyValuePair<string, string>("%27","'"),
            new KeyValuePair<string, string>("%28","("),
            new KeyValuePair<string, string>("%29",")"),
            new KeyValuePair<string, string>("%2A","*"),
            new KeyValuePair<string, string>("%2B","+")
        };

        #endregion
    }
}
