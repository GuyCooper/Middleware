using System.IO;

namespace MiddlewareInterfaces
{
    /// <summary>
    /// Interface defines a file request handler.
    /// </summary>
    public interface FileRequestHandler
    {
        /// <summary>
        /// Identifier of Handler.
        /// </summary>
        string Identifier { get;}

        /// <summary>
        /// Handle a file request. return true if request was handled otherwise return false.
        /// </summary>
        void HandleFileRequest(string query, Stream responseStream, out string contentType);
    }
}
