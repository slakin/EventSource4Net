using System;
using System.IO;
using System.Net;

namespace EventSource4Net
{
    class ServerResponse : IServerResponse
    {
        private readonly Stream _response;
        
        public ServerResponse(Stream response, Uri url)
        {
            ResponseUri = url;
            _response = response;
        }
        // Only to be backward compatible. HttpClient throws exceptions if not OK anyway.
        public HttpStatusCode StatusCode => HttpStatusCode.OK;

        public System.IO.Stream GetResponseStream()
        {
            return _response;
        }

        public Uri ResponseUri { get; }
    }
}
