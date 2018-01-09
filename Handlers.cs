using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleware
{
    interface IMessageHandler
    {
        bool ProcessMessage(MiddlewareMessage message);
        void AddHandler(IMessageHandler handler);
        void RemoveEndpoint(string id);
        IMessageHandler GetNext();
    }

    /// <summary>
    /// abstract base class for chaining command handlers. message passes through
    /// each handler in chain until it is handled
    /// </summary>
    abstract class CommandHandler : IMessageHandler
    {
        private IMessageHandler _Next = null;
        private string _Name;

        public CommandHandler(string name)
        {
            _Name = name;
        }

        protected abstract void HandleMessageInternal(MiddlewareMessage message);

        public bool ProcessMessage(MiddlewareMessage message)
        {
            if (message == null || message.Payload == null)
            {
                return false;
            }

            var payload = message.Payload;
            if (payload.Command == null)
            {
                return false;
            }

            if (payload.Command == _Name)
            {
                //this is the handler for this command
                //do stuff here
                HandleMessageInternal(message);
                return true;
            }

            return ProcessNextHandler(message);
        }

        public void AddHandler(IMessageHandler handler)
        {
            IMessageHandler nextAvailable = _Next;
            if(nextAvailable == null)
            {
                _Next = handler;
            }
            else
            {
                var previous = nextAvailable;
                while(nextAvailable != null)
                {
                    previous = nextAvailable;
                    nextAvailable = nextAvailable.GetNext();
                }
                previous.AddHandler(handler);
            }
        }

        public IMessageHandler GetNext()
        {
            return _Next;
        }

        protected bool ProcessNextHandler(MiddlewareMessage message)
        {
            if (_Next != null)
            {
                return _Next.ProcessMessage(message);
            }
            return false;
        }

        public abstract void RemoveEndpoint(string id);
    }

    /// <summary>
    /// abstract base class for handlers with channels
    /// </summary>
    abstract class ChannelCommandHandler : CommandHandler
    {
        protected IChannel _channel;

        public ChannelCommandHandler(string name, IChannel channel) : base(name)
        {
            _channel = channel;
        }

        public override void RemoveEndpoint(string id)
        {
            _channel.RemoveEndpoint(id);
        }

    }
    /// <summary>
    /// add a subscriber to a channel
    /// </summary>
    class SubscribeToChannelHandler : ChannelCommandHandler
    {
        public SubscribeToChannelHandler(IChannel channel) :
            base(HandlerNames.SUBSCRIBETOCHANNEL, channel)
        {
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.AddSubscriber(message);
        }
    }

    /// <summary>
    /// remove a subscriber froma channel
    /// </summary>
    class RemoveSubscriptionHandler : ChannelCommandHandler
    {
        public RemoveSubscriptionHandler(IChannel channel) :
            base(HandlerNames.REMOVESUBSCRIPTION, channel)
        {
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.RemoveSubscriber(message);
        }
    }

    /// <summary>
    /// send a message to a specified endpoint on the specified channel
    /// </summary>
    class SendMessageHandler : ChannelCommandHandler
    {
        public SendMessageHandler(IChannel channel) :
            base(HandlerNames.SENDMESSAGE, channel)
        {
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.SendMessage(message);
        }
    }

    /// <summary>
    /// add an endpoint to uniquely handle all requests on a channel
    /// </summary>
    class AddListenerHandler : ChannelCommandHandler
    {
        public AddListenerHandler(IChannel channel) :
            base(HandlerNames.ADDLISTENER, channel)
        {
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.AddListener(message);
        }
    }

    /// <summary>
    /// send a request to a channel. will only be handled if the channel
    /// has a primary request handler
    /// </summary>
    class SendRequestHandler : ChannelCommandHandler
    {
        public SendRequestHandler(IChannel channel) :
            base(HandlerNames.SENDREQUEST, channel)
        {
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.SendRequest(message);
        }
    }

    class PublishMessageHandler : ChannelCommandHandler
    {
        public PublishMessageHandler(IChannel channel) :
            base(HandlerNames.PUBLISHMESSAGE, channel)
        {
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.PublishMessage(message);
        }
    }
}
