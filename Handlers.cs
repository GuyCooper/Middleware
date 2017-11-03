using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleware
{
    interface IHandler
    {
        bool ProcessMessage(MiddlewareMessage message);
        void AddHandler(IHandler handler);
        void RemoveEndpoint(string id);
        IHandler GetNext();
    }

    abstract class CommandHandler : IHandler
    {
        private IHandler _Next = null;
        protected string _Name;
        protected IChannel _channel;

        public CommandHandler(IChannel channel)
        {
            _channel = channel;
        }

        protected abstract void HandleMessageInternal(MiddlewareMessage message);

        public bool ProcessMessage(MiddlewareMessage message)
        {
            if(message == null || message.Payload == null)
            {
                return false;
            }

            var payload = message.Payload;
            if(payload.Command == null)
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

            if (_Next != null)
            {
                return _Next.ProcessMessage(message);
            }
            return false;
        }

        public void AddHandler(IHandler handler)
        {
            IHandler nextAvailable = _Next;
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

        public IHandler GetNext()
        {
            return _Next;
        }
        public void RemoveEndpoint(string id)
        {
            _channel.RemoveEndpoint(id);
        }
    }

    /// <summary>
    /// add a subscriber to a channel
    /// </summary>
    class SubscribeToChannelHandler : CommandHandler
    {
        public SubscribeToChannelHandler(IChannel channel) :
            base(channel)
        {
            _Name = HandlerNames.SUBSCRIBETOCHANNEL;
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
    class RemoveSubscriptionHandler : CommandHandler
    {
        public RemoveSubscriptionHandler(IChannel channel) :
            base(channel)
        {
            _Name = HandlerNames.REMOVESUBSCRIPTION;
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
    class SendMessageHandler : CommandHandler
    {
        public SendMessageHandler(IChannel channel) :
            base(channel)
        {
            _Name = HandlerNames.SENDMESSAGE;
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
    class AddListenerHandler : CommandHandler
    {
        public AddListenerHandler(IChannel channel) :
            base(channel)
        {
            _Name = HandlerNames.ADDLISTENER;
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
    class SendRequestHandler : CommandHandler
    {
        public SendRequestHandler(IChannel channel) :
            base(channel)
        {
            _Name = HandlerNames.SENDREQUEST;
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.SendRequest(message);
        }
    }

    class PublishMessageHandler : CommandHandler
    {
        public PublishMessageHandler(IChannel channel) :
            base(channel)
        {
            _Name = HandlerNames.PUBLISHMESSAGE;
        }

        protected override void HandleMessageInternal(MiddlewareMessage message)
        {
            //subscribe this user to this channel
            _channel.PublishMessage(message);
        }
    }
}
