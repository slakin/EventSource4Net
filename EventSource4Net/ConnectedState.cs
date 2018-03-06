﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace EventSource4Net
{
    class ConnectedState : IConnectionState
    {
        private static readonly slf4net.ILogger _logger = slf4net.LoggerFactory.GetLogger(typeof(ConnectedState));

        private IWebRequesterFactory mWebRequesterFactory;
        private ServerSentEvent mSse = null;
        private string mRemainingText = string.Empty;   // the text that is not ended with a lineending char is saved for next call.
        private IServerResponse mResponse;
        private Dictionary<string, string> mHeaders;

        public EventSourceState State { get { return EventSourceState.OPEN; } }

        public ConnectedState(IServerResponse response, IWebRequesterFactory webRequesterFactory, Dictionary<string, string> headers)
        {
            mResponse = response;
            mWebRequesterFactory = webRequesterFactory;
            mHeaders = headers;
        }
        public async Task<IConnectionState> Run(Action<ServerSentEvent> msgReceived, CancellationToken cancelToken)
        {
            int i = 0;

            var stream = mResponse.GetResponseStream();
            {
                byte[] buffer = new byte[1024 * 8];
                var taskRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancelToken);


                if (!cancelToken.IsCancellationRequested)
                {
                    int bytesRead = taskRead;
                    if (bytesRead > 0) // stream has not reached the end yet
                    {
                        //Console.WriteLine("ReadCallback {0} bytesRead", bytesRead);
                        string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        text = mRemainingText + text;
                        string[] lines = StringSplitter.SplitIntoLines(text, out mRemainingText);
                        foreach (string line in lines)
                        {
                            if (cancelToken.IsCancellationRequested) break;

                            // Dispatch message if empty lne
                            if (string.IsNullOrEmpty(line.Trim()) && mSse != null)
                            {
                                msgReceived(mSse);
                                mSse = null;
                            }
                            else if (line.StartsWith(":"))
                            {
                                // This a comment, just log it.
                            }
                            else
                            {
                                string fieldName = String.Empty;
                                string fieldValue = String.Empty;
                                if (line.Contains(':'))
                                {
                                    int index = line.IndexOf(':');
                                    fieldName = line.Substring(0, index);
                                    fieldValue = line.Substring(index + 1).TrimStart();
                                }
                                else
                                    fieldName = line;

                                if (String.Compare(fieldName, "event", true) == 0)
                                {
                                    mSse = mSse ?? new ServerSentEvent();
                                    mSse.EventType = fieldValue;
                                }
                                else if (String.Compare(fieldName, "data", true) == 0)
                                {
                                    mSse = mSse ?? new ServerSentEvent();
                                    mSse.Data = fieldValue + '\n';
                                }
                                else if (String.Compare(fieldName, "id", true) == 0)
                                {
                                    mSse = mSse ?? new ServerSentEvent();
                                    mSse.LastEventId = fieldValue;
                                }
                                else if (String.Compare(fieldName, "retry", true) == 0)
                                {
                                    int parsedRetry;
                                    if (int.TryParse(fieldValue, out parsedRetry))
                                    {
                                        mSse = mSse ?? new ServerSentEvent();
                                        mSse.Retry = parsedRetry;
                                    }
                                }
                                else
                                {
                                    // Ignore this, just log it
                                }
                            }
                        }

                        if (!cancelToken.IsCancellationRequested)
                            return this;
                    }
                    else // end of the stream reached
                    {
                                _logger.Trace("No bytes read. End of stream.");
                    }
                }
                return new DisconnectedState(mResponse.ResponseUri, mWebRequesterFactory, mHeaders);
            }
        }

    }
}
