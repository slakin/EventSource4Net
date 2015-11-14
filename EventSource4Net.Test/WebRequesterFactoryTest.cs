using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventSource4Net.Test
{
    class WebRequesterFactoryMock : IWebRequesterFactory
    {
        public WebRequesterMock WebRequesterMock
        {
            get;
            private set;
        }
        public WebRequesterFactoryMock(ServiceResponseMock response)
        {
            this.WebRequesterMock = new WebRequesterMock(response);
        }
        public IWebRequester Create()
        {
            return WebRequesterMock;
        }
    }

    class WebRequesterMock : IWebRequester
    {
        public ManualResetEvent GetCalled = new ManualResetEvent(false);
        public ServiceResponseMock Response { get; private set; }

        public WebRequesterMock(ServiceResponseMock response)
        {
            this.Response = response;
        }

        public System.Threading.Tasks.Task<IServerResponse> Get(Uri url)
        {
            return Task.Factory.StartNew<IServerResponse>(() =>
            {
                GetCalled.Set();
                return Response;
            });
        }
    }

    class ServiceResponseMock : IServerResponse
    {
        private TestableStream mStream;
        private StreamWriter mStreamWriter;
        private Uri mUrl;
        private HttpStatusCode mStatusCode;

        public ManualResetEvent StatusCodeCalled = new ManualResetEvent(false);

        public ServiceResponseMock(Uri url, HttpStatusCode statusCode)
        {
            mUrl = url;
            mStatusCode = statusCode;
            mStream = new TestableStream();
            mStreamWriter = new StreamWriter(mStream);
        }

        public System.Net.HttpStatusCode StatusCode
        {
            get
            {
                StatusCodeCalled.Set();
                return mStatusCode;
            }
        }

        public System.IO.Stream GetResponseStream()
        {
            return mStream;
        }

        public Uri ResponseUri
        {
            get { return mUrl; }
        }

        public void WriteTestTextToStream(string text)
        {
            mStreamWriter.Write(text);
            mStreamWriter.Flush();
        }

        public void DistantConnectionClose()
        {
            mStream.Throws(new SocketException(10054));
        }
    }

    class GetIsCalledEventArgs : EventArgs
    {
        public ServiceResponseMock ServerResponse { get; private set; }
        public GetIsCalledEventArgs(ServiceResponseMock response)
        {
            ServerResponse = response;
        }
    }


    class TestableStream : Stream
    {
        long _pos = 0;
        System.Collections.Concurrent.BlockingCollection<string> _texts = new System.Collections.Concurrent.BlockingCollection<string>();
        private CancellationTokenSource _cancellationTokenSource;
        private Exception _throw;

        public TestableStream()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {

        }

        public override long Length
        {
            get { return _texts.Count(); }
        }

        public override long Position
        {
            get
            {
                return _pos;
            }
            set
            {
                _pos = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                var s = _texts.Take(_cancellationTokenSource.Token);
                byte[] encodedText = Encoding.UTF8.GetBytes(s);
                encodedText.CopyTo(buffer, offset);
                return encodedText.Length;
            }
            catch (OperationCanceledException)
            {
                if (_throw != null)
                {
                    var ex = _throw;
                    _throw = null;
                    throw ex;
                }
                return 0;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            string s = Encoding.UTF8.GetString(buffer, offset, count);
            _texts.Add(s);
            //_texts.CompleteAdding();
        }

        public void Throws(Exception exception)
        {
            _cancellationTokenSource.Cancel();
            _throw = exception;
        }
    }


}
