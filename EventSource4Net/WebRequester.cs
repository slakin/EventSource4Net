using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EventSource4Net
{
    class WebRequester : IWebRequester
    {
        public static WebRequest _WebRequest { get; set; }
        private HttpWebRequest wreq { get; set; }

        public Task<IServerResponse> Get(Uri url)
        {
            _WebRequest = WebRequest.Create(url);
            wreq = (HttpWebRequest)_WebRequest;
            wreq.Method = "GET";
            wreq.Proxy = null;

            var taskResp = Task.Factory.FromAsync<WebResponse>(wreq.BeginGetResponse,
                                                            wreq.EndGetResponse,
                                                            null).ContinueWith<IServerResponse>(t => new ServerResponse(t.Result));
            return taskResp;

        }
    }
}
