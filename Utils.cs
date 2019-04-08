using System.Reflection;
using System.IO;

namespace Middleware
{
    /// <summary>
    /// Utility class.
    /// </summary>
    internal static class Utils
    {
        public static Assembly LoadAssembley(string file)
        {
            if(string.IsNullOrEmpty(Path.GetDirectoryName(file)))
            {
                //determine if this file is in the local folder
                return Assembly.LoadFrom(file);
            }
            return Assembly.LoadFile(file);
        }
    }
}
