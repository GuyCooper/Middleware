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
        void AddSubscriber(Message message); //add a subscriber to a channel to receive any broadcasts
        void RemoveSubscriber(Message message); //remove subscriber
        void SendMessage(Message message); //send a message to a specified recipient
        void AddListener(Message message); //add a listener to handle all requests on a channel
        void SendRequest(Message message); //send a request to a channel, must have a listener to be processed
        void PublishMessage(Message message); //broadcast a message to a channel, will be handled by all subscribers
        void RemoveEndpoint(string id); //remove endpoint from all channels
    }
    class Channel :IChannel
    {
        private Dictionary<string, IEndpoint> _subscribers = new Dictionary<string, IEndpoint>();
        private Dictionary<string, IEndpoint> _requesters = new Dictionary<string, IEndpoint>();
        private IEndpoint _PrimaryRequestHandler = null;
       

        public string Name { get; set; }

        private IEndpoint _GetSubscriber(Message message)
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
        public void AddSubscriber(Message message)
        {
            var subscriber = _GetSubscriber(message);
            if(_subscribers.ContainsKey(subscriber.Id) == false)
            {
                _subscribers.Add(subscriber.Id, subscriber);
            }
        }

        public void RemoveSubscriber(Message message)
        {
            var subscriber = _GetSubscriber(message);
            if (_subscribers.ContainsKey(subscriber.Id) == true)
            {
                _subscribers.Remove(subscriber.Id);
            }
        }

        //send a message to a speciifed recipient
        public void SendMessage(Message message)
        {
            //destination specified
            IEndpoint destination;
            if (message.DestinationId != null && _requesters.TryGetValue(message.DestinationId, out destination) == true)
            {
                destination.SendData(message);
            }
            else
            {
                throw new InvalidDestinationException(Name, message.DestinationId);
            }
        }

        public void AddListener(Message message)
        {
            var source = _GetSubscriber(message);
            _PrimaryRequestHandler = source;
        }

        public void SendRequest(Message message)
        {
            if(_PrimaryRequestHandler != null)
            {
                message.DestinationId = _PrimaryRequestHandler.Id;
                if(message.Source == null)
                {
                    throw new InvalidSourceException(Name);
                }

                var lookup = message.SourceId ?? message.Source.Id;

                if(_requesters.ContainsKey(lookup) == false)
                {
                    _requesters.Add(lookup, message.Source);
                }

                _PrimaryRequestHandler.SendData(message);
            }
            else
            {
                throw new MissingListenerException(Name);
            }
        }

        //broadcast message to al listeners
        public void PublishMessage(Message message)
        {
            //no destination send to all subscribers
            foreach(var IEndpoint in _subscribers.Values)
            {
                IEndpoint.SendData(message);
            }
        }
        public void RemoveEndpoint(string id)
        {
            _subscribers.Remove(id);
            _requesters.Remove(id);
            if((_PrimaryRequestHandler != null)&&(_PrimaryRequestHandler.Id == id))
            {
                _PrimaryRequestHandler = null;
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

        private void _ProcessChannelCommand(Message message, Action<IChannel> command)
        {
            var source = message.Source;
            try
            {
                var channel = _getchannel(message.Channel);
                command(channel);
                if (_stats != null)
                {
                    _stats.UpdateChannelStats(message);
                }
            }
            catch(Exception e)
            {
                
                if(source != null)
                {
                    source.OnError(message, e.Message);
                }
                return;
            }

            if(source != null)
            {
                source.OnSucess(message);
            }
        }

        public async void AddListener(Message message)
        {
            await _taskFactory.StartNew(() => 
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.AddListener(message);
                });
            });
        }

        public async void AddSubscriber(Message message)
        {
            await _taskFactory.StartNew(() =>
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.AddSubscriber(message);
                });
            });
        }

        public async void RemoveSubscriber(Message message)
        {
            await _taskFactory.StartNew(() =>
            {
                _channelLookup.Remove(message.Channel);
                var source = message.Source;
                if(source != null)
                {
                    source.OnSucess(message);
                }
            });
        }

        public async void SendMessage(Message message)
        {
            await _taskFactory.StartNew(() =>
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.SendMessage(message);
                });
            });
        }

        public async void SendRequest(Message message)
        {
            await _taskFactory.StartNew(() =>
            {
                _ProcessChannelCommand(message, (channel) =>
                {
                    channel.SendRequest(message);
                });
            });
        }

        public async void PublishMessage(Message message)
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
