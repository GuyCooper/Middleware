using System;
using MiddlewareInterfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

namespace Middleware
{
    /// <summary>
    /// Class manages http file requests.
    /// </summary>
    internal class FileRequestManager
    {
        #region Public Methods

        /// <summary>
        /// Constructor. Service locate all file request handlers.
        /// </summary>
        public FileRequestManager(string fileRequesterPath)
        {
            LocateAllRequestHandlers(fileRequesterPath);
        }

        /// <summary>
        /// Handle a file handler request.
        /// </summary>
        public bool HandleFileRequest(string identifier, string query, Stream responseStream, out string contentType)
        {
            var handler = m_fileHandlers.FirstOrDefault(h => h.Identifier == identifier);
            if(handler != null)
            {
                logger.Info($"processing request with handler {identifier} for query {query}");
                handler.HandleFileRequest(query, responseStream, out contentType);
                return true;
            }
            else
            {
                logger.Error($"No handler to process request {identifier}");
                contentType = null;
            }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Locate all File Request Handlers.
        /// </summary>
        private void LocateAllRequestHandlers(string fileRequesterPath)
        {
            var files = Directory.EnumerateFiles(fileRequesterPath, "*.dll");
            foreach(var file in files)
            {
                try
                {
                    var assembley = Utils.LoadAssembley(file);
                    var handlers = assembley.GetTypes().Where(t =>
                    {
                        return IsFileRequestHandler(t);
                    });

                    foreach (var handler in handlers)
                    {
                        var fileHandler = (FileRequestHandler)Activator.CreateInstance(handler);
                        logger.Info($"Adding file request handler {fileHandler.Identifier}");
                        m_fileHandlers.Add(fileHandler);
                    }
                }
                catch(Exception ex)
                {
                    logger.Info($"Error loading dll {file}. Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Return true if this type is a FileRequestHandler
        /// </summary>
        private bool IsFileRequestHandler(Type t)
        {
            return t.IsAbstract == false && 
                   t.GetInterfaces().FirstOrDefault(s => s == typeof(FileRequestHandler)) != null;
        }

        #endregion

        #region Private Data

        private List<FileRequestHandler> m_fileHandlers = new List<FileRequestHandler>();

        //logger instance
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

    }
}
