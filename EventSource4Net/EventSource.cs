﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace EventSource4Net
{
    public class EventSource
    {
        private static readonly slf4net.ILogger _logger = slf4net.LoggerFactory.GetLogger(typeof(EventSource));

        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<ServerSentEventReceivedEventArgs> EventReceived;

        public CancellationTokenSource CancellationToken { get; set; }

        private IWebRequesterFactory _webRequesterFactory = new WebRequesterFactory();
        private int _timeout = 0;
        public Uri Url { get; private set; }
        public EventSourceState State { get { return CurrentState.State; } }
        public string LastEventId { get; private set; }
        private IConnectionState mCurrentState = null;
        private CancellationToken mStopToken;
        private CancellationTokenSource mTokenSource = new CancellationTokenSource();
        private Dictionary<string, string> _headers;
        private Uri url;
        private IWebRequesterFactory factory;
        private Dictionary<string, string> headers;

        private IConnectionState CurrentState
        {
            get { return mCurrentState; }
            set
            {
                if (!value.Equals(mCurrentState))
                {
                    StringBuilder sb = new StringBuilder("State changed from ");
                    sb.Append(mCurrentState == null ? "Unknown" : mCurrentState.State.ToString());
                    sb.Append(" to ");
                    sb.Append(value == null ? "Unknown" : value.State.ToString());
                    _logger.Trace(sb.ToString());
                    mCurrentState = value;
                    OnStateChanged(mCurrentState.State);
                }
            }
        }

        public EventSource(Uri url, int timeout)
        {
            Initialize(url, timeout);
        }

        public EventSource(Uri url, Dictionary<string, string> headers, int timeout)
        {
            _headers = headers;
            Initialize(url, timeout);
        }

        /// <summary>
        /// Constructor for testing purposes
        /// </summary>
        /// <param name="factory">The factory that generates the WebRequester to use.</param>
        protected EventSource(Uri url, IWebRequesterFactory factory)
        {
            _webRequesterFactory = factory;
            Initialize(url, 0);
        }

        protected EventSource(Uri url, IWebRequesterFactory factory, Dictionary<string, string> headers)
        {
            _webRequesterFactory = factory;
            _headers = headers;
            Initialize(url, 0);
        }

        private void Initialize(Uri url, int timeout)
        {
            _timeout = timeout;
            Url = url;
            CurrentState = new DisconnectedState(Url, _webRequesterFactory, _headers);
            _logger.Info("EventSource created for " + url.ToString());
        }


        /// <summary>
        /// Start the EventSource. 
        /// </summary>
        /// <param name="stopToken">Cancel this token to stop the EventSource.</param>
        public void Start(CancellationToken stopToken)
        {
            if (State == EventSourceState.CLOSED)
            {
                mStopToken = stopToken;
                mTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
                Run();
            }
        }

        protected void Run()
        {
            if (mTokenSource.IsCancellationRequested && CurrentState.State == EventSourceState.CLOSED)
                return;

            mCurrentState.Run(this.OnEventReceived, mTokenSource.Token).ContinueWith(cs =>
            {
                CurrentState = cs.Result;
                Run();
            });
        }

        protected void OnEventReceived(ServerSentEvent sse)
        {
            if (EventReceived != null)
            {
                EventReceived(this, new ServerSentEventReceivedEventArgs(sse));
            }
        }

        protected void OnStateChanged(EventSourceState newState)
        {
            if (StateChanged != null)
            {
                StateChanged(this, new StateChangedEventArgs(newState));
            }
        }
    }
}
