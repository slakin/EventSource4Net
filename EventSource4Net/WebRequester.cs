using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EventSource4Net
{
    public class WebRequester : IWebRequester
    {
        static WebRequester()
        {
            ServicePointManager.DefaultConnectionLimit = 100000;

            var http = new HttpClient();
            
            http.DefaultRequestHeaders.Add("Accept", "text/event-stream");
            http.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br");

            Http = http;

        }

        static HttpClient Http;

        /// <summary>
        /// Override default static HttpClient instance
        /// </summary>
        /// <param name="httpClient"></param>
        public static void OverrideHttpClient(HttpClient httpClient)
        {
            Http = httpClient;
        }

        public Task<IServerResponse> Get(Uri url, Dictionary<string, string> headers = null)
        {
            return Http.GetStreamAsync(url).ContinueWith<IServerResponse>(t => new ServerResponse(t.Result, url));

        }
    }
}
