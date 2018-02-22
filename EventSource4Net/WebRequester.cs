using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace EventSource4Net
{
    class WebRequester : IWebRequester
    {
        public static WebRequest _WebRequest { get; set; }
        private HttpWebRequest wreq { get; set; }

        public Task<IServerResponse> Get(Uri url, Dictionary<string, string> headers = null)
        {
            _WebRequest = WebRequest.Create(url);
            wreq = (HttpWebRequest)_WebRequest;
            wreq.Method = "GET";
            wreq.Proxy = null;

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    wreq.Headers.Add(header.Key, header.Value);
                }
            }

            var taskResp = Task.Factory.FromAsync<WebResponse>(wreq.BeginGetResponse,
                                                            wreq.EndGetResponse,
                                                            null).ContinueWith<IServerResponse>(t => new ServerResponse(t.Result));
            return taskResp;

        }
    }
}
