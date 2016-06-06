using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

namespace WebUser.PolicyAuthHelpers
{
    class HttpDocumentRetriever : IDocumentRetriever
    {
        private readonly HttpClient _httpClient;

        public HttpDocumentRetriever() : this(new HttpClient())
        {

        }

        public HttpDocumentRetriever(HttpClient httpClient)
        {
            if(httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            _httpClient = httpClient;
        }

        public async Task<string> GetDocumentAsync(string address, CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentNullException("address");
            }
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(address, cancel).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new IOException("Unable to get document from: " + address, ex);
            }
        }
    }
}