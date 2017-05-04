using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace EventSource4Net
{
    class ConnectingState : IConnectionState
    {
        private static readonly slf4net.ILogger _logger = slf4net.LoggerFactory.GetLogger(typeof(ConnectingState));

        private Uri mUrl;
        private IWebRequesterFactory mWebRequesterFactory;
        private Dictionary<string, string> mHeaders;

        public EventSourceState State { get { return EventSourceState.CONNECTING; } }
        
        public ConnectingState(Uri url, IWebRequesterFactory webRequesterFactory, Dictionary<string, string> headers)
        {
            if (url == null) throw new ArgumentNullException("Url cant be null");
            if (webRequesterFactory == null) throw new ArgumentNullException("Factory cant be null");
            mUrl = url;
            mWebRequesterFactory = webRequesterFactory;
            mHeaders = headers;
        }

        public Task<IConnectionState> Run(Action<ServerSentEvent> donothing, CancellationToken cancelToken)
        {
            IWebRequester requester = mWebRequesterFactory.Create();
            var taskResp = requester.Get(mUrl, mHeaders);

            return taskResp.ContinueWith<IConnectionState>(tsk => 
            {
                if (tsk.Status == TaskStatus.RanToCompletion && !cancelToken.IsCancellationRequested)
                {
                    IServerResponse response = tsk.Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return new ConnectedState(response, mWebRequesterFactory, mHeaders);
                    }
                    else
                    {
                        _logger.Info("Failed to connect to: " + mUrl.ToString() + response ?? (" Http statuscode: " + response.StatusCode));
                    }
                }

                return new DisconnectedState(mUrl, mWebRequesterFactory, mHeaders);
            });
        }
    }
}
