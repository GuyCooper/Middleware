using System;
using MiddlewareNetClient;
using System.Linq;
using System.Threading.Tasks;

namespace MiddlewareTestConsoleApp
{
    class Program
    {
        static void RequestListeners(ISession session, int start, int increment)
        {
            if (session != null)
            {
                var channels = Enumerable.Range(start, increment).Select(i => $"TEST_CHANNEL_{i}").ToList();
                foreach (var channel in channels)
                {
                    Console.WriteLine($"listening on channel {channel}.");
                    Task.Factory.StartNew(() => {
                    _middleware.AddChannelListener(session, channel).ContinueWith(t =>
                    {
                        var result = t.Result;
                        if (result.Success)
                        {
                            Console.WriteLine($"request succeded: {result.Payload} ");
                        }
                        else
                        {
                            Console.WriteLine($"request failed: {result.Payload}");
                        }
                    });
                    });
                    //System.Threading.Thread.Sleep(50);
                }
            }
        }

        static void Main(string[] args)
        {
            var nextBatchEvent = new System.Threading.AutoResetEvent(false);
            var shutdownEvent = new System.Threading.ManualResetEvent(false);
            Console.WriteLine("connecting to server...");

            var sessionTask = ConnectToServer();

            var session = sessionTask.Result;

            if (session == null)
            {
                Console.WriteLine("failed to connect to server :(");
                return;
            }

           Console.WriteLine("connected!!. listening to channels");

            int count = 1;
            int increment = 10;

            while (shutdownEvent.WaitOne(1000) == false)
            {
                if(nextBatchEvent.WaitOne(1000) == true)
                {
                    RequestListeners(session, count, increment);
                    count += increment;
                }

                Console.WriteLine("press any to request next batch or s to stop....");
                var entered = Console.ReadLine();
                if (entered == "s")
                {
                    shutdownEvent.Set();
                }
                else
                {
                    nextBatchEvent.Set();
                }
            }



            Console.WriteLine("shutting down...");
            var disposable = session as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }

        }

        static async Task<ISession> ConnectToServer()
        {
            return await _middleware.CreateSession("ws://localhost:8080", "admin", "password", "Test Console App");
        }

        private static MiddlewareManager _middleware = new MiddlewareManager();
    }
}
