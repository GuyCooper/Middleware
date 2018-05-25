
namespace Middleware
{
    /// <summary>
    /// Defines an interface to handle all messages received by the middleware service.
    /// </summary>
    interface IMessageHandler
    {
        bool ProcessMessage(MiddlewareMessage message);
        void AddHandler(IMessageHandler handler);
        void RemoveEndpoint(string id);
        IMessageHandler GetNext();
    }

    /// <summary>
    /// Abstract base class for chaining command handlers. message passes through
    /// each handler in chain until it is handled
    /// </summary>
    abstract class CommandHandler : IMessageHandler
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public CommandHandler(string name)
        {
            _Name = name;
        }

        /// <summary>
        /// Process a message on this handler. returns true if message was processed by this
        /// handler otherwise returns false.
        /// </summary>
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

        /// <summary>
        /// Add a message handler to the chain. Message handler is implemented as a singly
        /// linked list.
        /// </summary>
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

        /// <summary>
        /// Return next message handler in the chain.
        /// </summary>
        public IMessageHandler GetNext()
        {
            return _Next;
        }

        /// <summary>
        /// Abstract method called when endpoint is closed.
        /// </summary>
        /// <param name="id"></param>
        public abstract void RemoveEndpoint(string id);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Internal method for handling a message on this handler.
        /// </summary>
        protected abstract void HandleMessageInternal(MiddlewareMessage message);

        /// <summary>
        /// Process message on next handler in chain if it exists. Otherewise
        /// return false.
        /// </summary>
        protected bool ProcessNextHandler(MiddlewareMessage message)
        {
            if (_Next != null)
            {
                return _Next.ProcessMessage(message);
            }
            return false;
        }

        #endregion

        #region Private Data Members

        // Next handler in chain.
        private IMessageHandler _Next = null;

        // Name of this handler
        private string _Name;

        #endregion
    }

    /// <summary>
    /// Abstract base class for handlers with channels
    /// </summary>
    abstract class ChannelCommandHandler : CommandHandler
    {
        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChannelCommandHandler(string
            name, IChannel channel) : base(name)
        {
            _channel = channel;
        }

        /// <summary>
        /// Called when an endpoint is closed
        /// </summary>
        public override void RemoveEndpoint(string id)
        {
            _channel.RemoveEndpoint(id);
        }

        #endregion

        #region Protected Data Members

        protected IChannel _channel;

        #endregion
    }
    /// <summary>
    /// Add a subscriber to a channel handler.
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
    /// Remove a subscriber froma channel handler.
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
    /// Send a message to a specified endpoint on the specified channel handler.
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
    /// Add an endpoint to uniquely handle all requests on a channel handler.
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
    /// Send a request to a channel. will only be handled if the channel
    /// has a primary request handler.
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

    /// <summary>
    /// Public message handler. Message is published to all subscribers on this channel
    /// </summary>
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
