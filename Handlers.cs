using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Middleware
{
    interface IHandler
    {
        bool ProcessMessage(Message message);
        void AddHandler(IHandler handler);
        void RemoveEndpoint(string id);
        IHandler GetNext();
    }

    internal static class HandlerNames
    {
        public static readonly string SUBSCRIBETOCHANNEL = "SUBSCRIBETOCHANNEL";
        public static readonly string REMOVESUBSCRIPTION = "REMOVESUBSCRIPTION";
        public static readonly string SENDMESSAGE = "SENDMESSAGE";
        public static readonly string ADDLISTENER = "ADDLISTENER";
        public static readonly string SENDREQUEST = "SENDREQUEST";
        public static readonly string PUBLISHMESSAGE = "PUBLISHMESSAGE";
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

        protected abstract void HandleMessageInternal(Message message);

        public bool ProcessMessage(Message message)
        {
            if(message == null || message.Command == null)
            {
                return false;
            }

            if (message.Command == _Name)
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

        protected override void HandleMessageInternal(Message message)
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

        protected override void HandleMessageInternal(Message message)
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

        protected override void HandleMessageInternal(Message message)
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

        protected override void HandleMessageInternal(Message message)
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

        protected override void HandleMessageInternal(Message message)
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

        protected override void HandleMessageInternal(Message message)
        {
            //subscribe this user to this channel
            _channel.PublishMessage(message);
        }
    }
}
