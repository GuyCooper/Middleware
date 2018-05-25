using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleware
{
    internal abstract class MiddlewareException : Exception
    {
        private string _name;
        private string _reason;
        public MiddlewareException(string name, string reason)
        {
            _name = name;
            _reason = reason;
        }
        public override string Message
        {
            get { return string.Format("error on entity {0}. {1}.", _name, _reason); }
        }
    }

    internal class MissingListenerException : MiddlewareException
    {
        public MissingListenerException(string name) : base(name, "No Listener specified for channel")
        {
        }
    }

    internal class InvalidDestinationException : MiddlewareException
    {
        public InvalidDestinationException(string name, string destination) : 
            base(name, "Invalid Destination endpoint : " + destination)
        {
        }
    }

    internal class InvalidSourceException : MiddlewareException
    {
        public InvalidSourceException(string name) :
            base(name, "Invalid Source endpoint")
        {
        }
    }

    interface IChannel
    {
        void AddSubscriber(MiddlewareMessage message); //add a subscriber to a channel to receive any broadcasts
        void RemoveSubscriber(MiddlewareMessage message); //remove subscriber
        void SendMessage(MiddlewareMessage message); //send a message to a specified recipient
        void AddListener(MiddlewareMessage message); //add a listener to handle all requests on a channel
        void SendRequest(MiddlewareMessage message); //send a request to a channel, must have a listener to be processed
        void PublishMessage(MiddlewareMessage message); //broadcast a message to a channel, will be handled by all subscribers
        void RemoveEndpoint(string id); //remove endpoint from all channels
    }
    class Channel :IChannel
    {
        private Dictionary<string, IEndpoint> _subscribers = new Dictionary<string, IEndpoint>();
        private IEndpoint _PrimaryRequestHandler = null;

        public string Name { get; set; }

        private IEndpoint _GetSubscriber(MiddlewareMessage message)
        {
            if(message == null)
            {
                throw new ArgumentException("message not defined");
            }
            if(message.Source == null)
            {
                throw new ArgumentException("message source not defined");
            }
            return message.Source;
        }
        public void AddSubscriber(MiddlewareMessage message)
        {
            var subscriber = _GetSubscriber(message);
            if(_subscribers.ContainsKey(subscriber.Id) == false)
            {
                _subscribers.Add(subscriber.Id, subscriber);
            }
        }

        public void RemoveSubscriber(MiddlewareMessage message)
        {
            var subscriber = _GetSubscriber(message);
            if (_subscribers.ContainsKey(subscriber.Id) == true)
            {
                _subscribers.Remove(subscriber.Id);
            }
        }

        //send a message to a speciifed recipient
        public void SendMessage(MiddlewareMessage message)
        {
            //destination specified
            var payload = message.Payload;
            IEndpoint destination;
            if (payload.DestinationId != null && _subscribers.TryGetValue(payload.DestinationId, out destination) == true)
            {
                destination.SendData(payload);
            }
            else
            {
                throw new InvalidDestinationException(Name, payload.DestinationId);
            }
        }

        public void AddListener(MiddlewareMessage message)
        {
            var source = _GetSubscriber(message);
            _PrimaryRequestHandler = source;
        }

        public void SendRequest(MiddlewareMessage message)
        {
            var payload = message.Payload;
            if(_PrimaryRequestHandler != null)
            {
                payload.DestinationId = _PrimaryRequestHandler.Id;
                if(message.Source == null)
                {
                    throw new InvalidSourceException(Name);
                }

                var lookup = payload.SourceId ?? message.Source.Id;

                _PrimaryRequestHandler.SendData(payload);
            }
            else
            {
                throw new MissingListenerException(Name);
            }
        }

        //broadcast message to al listeners
        public void PublishMessage(MiddlewareMessage message)
        {
            var payload = message.Payload;
            //no destination send to all subscribers
            foreach(var IEndpoint in _subscribers.Values)
            {
                IEndpoint.SendData(payload);
            }
        }
        public void RemoveEndpoint(string id)
        {
            _subscribers.Remove(id);
            if(_PrimaryRequestHandler != null)
            {
                if (_PrimaryRequestHandler.Id == id)
                {
                    _PrimaryRequestHandler = null;
                }
                else
                {
                    //inform the primary request handler that a session is closing
                   
                }
            }
        }
    }

    class Channels : IChannel
    {
        private Dictionary<string, IChannel> _channelLookup = new Dictionary<string, IChannel>();

        private LimitedConcurrencyLevelTaskScheduler _scheduler;
        private TaskFactory _taskFactory;
        private IMessageStats _stats;

        public Channels(IMessageStats stats)
        {
            //all methods in this class need to be executed on a single thread
            _stats = stats;
            _scheduler = new LimitedConcurrencyLevelTaskScheduler(1);
            _taskFactory = new TaskFactory(_scheduler);
        }

        private IChannel _getchannel(string name)
        {
            IChannel channel;

            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("no channel specified!");
            }

            if (_channelLookup.TryGetValue(name, out channel) == false)
            {
                channel = new Channel();
                _channelLookup.Add(name, channel);
            }
            return channel;
        }

        private void _ProcessChannelCommand(MiddlewareMessage message, Action<IChannel> command)
        {
            var source = message.Source;
            var payload = message.Payload;
            try
            {
                var channel = _getchannel(payload.Channel);
                command(channel);
                if (_stats != null)
                {
                    _stats.UpdateChannelStats(payload);
                }
            }
            catch(Exception e)
            {
                
                if(source != null)
                {
                    source.OnError(payload, e.Message);
                }
                return;
            }

            if(source != null)
            {
                source.OnSucess(payload);
            }
        }

        public async void AddListener(MiddlewareMessage message)
        {
            await _taskFactory.StartNew(() => 
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.AddListener(message);
                });
            });
        }

        public async void AddSubscriber(MiddlewareMessage message)
        {
            await _taskFactory.StartNew(() =>
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.AddSubscriber(message);
                });
            });
        }

        public async void RemoveSubscriber(MiddlewareMessage message)
        {
            await _taskFactory.StartNew(() =>
            {
                var payload = message.Payload;
                _channelLookup.Remove(payload.Channel);
                var source = message.Source;
                if(source != null)
                {
                    source.OnSucess(payload);
                }
            });
        }

        public async void SendMessage(MiddlewareMessage message)
        {
            await _taskFactory.StartNew(() =>
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.SendMessage(message);
                });
            });
        }

        public async void SendRequest(MiddlewareMessage message)
        {
            await _taskFactory.StartNew(() =>
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.SendRequest(message);
                });
            });
        }

        public async void PublishMessage(MiddlewareMessage message)
        {
            await _taskFactory.StartNew(() =>
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.PublishMessage(message);
                });
            });
        }

        public async void RemoveEndpoint(string id)
        {
            await _taskFactory.StartNew(() =>
            {
                foreach (var channel in _channelLookup)
                {
                    channel.Value.RemoveEndpoint(id);
                }
            });
        }
    }
}
