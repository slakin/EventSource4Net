using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventSource4Net
{
    class DisconnectedState : IConnectionState 
    {
        private Uri mUrl;
        private IWebRequesterFactory mWebRequesterFactory;
        private Dictionary<string, string> headers;

        public EventSourceState State
        {
            get { return EventSourceState.CLOSED; }
        }

        public DisconnectedState(Uri url, IWebRequesterFactory webRequesterFactory, Dictionary<string, string> headers)
        {
            if (url == null) throw new ArgumentNullException("Url cant be null");
            mUrl = url;
            mWebRequesterFactory = webRequesterFactory;
            this.headers = headers;
        }

        public Task<IConnectionState> Run(Action<ServerSentEvent> donothing, CancellationToken cancelToken, Dictionary<string, string> headers)
        {
            if(cancelToken.IsCancellationRequested)
                return Task.Factory.StartNew<IConnectionState>(() => { return new DisconnectedState(mUrl, mWebRequesterFactory, headers); });
            else
                return Task.Factory.StartNew<IConnectionState>(() => { return new ConnectingState(mUrl, mWebRequesterFactory, headers); });
        }
    }
}
