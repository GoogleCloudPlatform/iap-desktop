using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Util
{
    internal class RestClient
    {
        private readonly string userAgent;

        public RestClient(string userAgent)
        {
            this.userAgent = userAgent;
        }

        public async Task<TModel> GetAsync<TModel>(string url, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd(this.userAgent);
                using (var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    using (var reader = new StreamReader(stream))
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        return new JsonSerializer().Deserialize<TModel>(jsonReader);
                    }
                }
            }
        }
    }

    [Serializable]
    public class RestClientException : Exception
    {
        protected RestClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public RestClientException(string message) : base(message)
        {
        }
    }
}
