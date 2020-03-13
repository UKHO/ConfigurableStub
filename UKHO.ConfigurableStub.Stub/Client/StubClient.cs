// British Crown Copyright © 2020,
// All rights reserved.
// 
// You may not copy the Software, rent, lease, sub-license, loan, translate, merge, adapt, vary
// re-compile or modify the Software without written permission from UKHO.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL CROWN OR THE SECRETARY OF STATE FOR DEFENCE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
// OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UKHO.ConfigurableStub.Stub.Models;

namespace UKHO.ConfigurableStub.Stub.Client
{
    /// <summary>
    ///     A client that configures the stub via HTTP requests.
    /// </summary>
    public class StubClient : IDisposable
    {
        private readonly string _stubBaseUrl;
        private readonly HttpClient _httpClient;

        /// <summary>
        ///     Constructor for the StubClient.
        /// </summary>
        /// <param name="stubBaseUrl">The base URL of a running UKHO.ConfigurableStub.</param>
        public StubClient(string stubBaseUrl)
        {
            _stubBaseUrl = stubBaseUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        ///     Get last request made to the UKHO.ConfigurableStub on a specified route.
        /// </summary>
        /// <param name="verb">The HTTP verb for the request.</param>
        /// <param name="route">The HTTP route for the request.</param>
        /// <typeparam name="T">An object that represents the request body.</typeparam>
        /// <returns>Returns an option of request record on success or an empty option on failure.</returns>
        public async Task<Option<RequestRecord<T>>> GetLastRequestAsync<T>(HttpMethod verb, string route)
        {
            route = route.TrimStart('/');
            SetServicePointManagerSecurityProtocol();
            var requestUri = $"{_stubBaseUrl}/Stub/{verb}/api/{route}";
            return await GetValueFromStub<RequestRecord<T>>(requestUri);
        }

        /// <summary>
        ///     Get the history of the requests sent to a specific route
        /// </summary>
        /// <param name="verb">The HTTP verb for the request</param>
        /// <param name="route">The HTTP route for the request</param>
        /// <typeparam name="T">An object that represents the request body</typeparam>
        /// <returns>Returns an option of request record on success or an empty option on failure.</returns>
        public async Task<Option<List<RequestRecord<T>>>> GetRequestHistoryAsync<T>(HttpMethod verb, string route)
        {
            route = route.TrimStart('/');
            SetServicePointManagerSecurityProtocol();
            var requestUri = $"{_stubBaseUrl}/Stub/{verb}/history/api/{route}";
            return await GetValueFromStub<List<RequestRecord<T>>>(requestUri);
        }

        /// <summary>
        ///     Configure a route in the stub.
        /// </summary>
        /// <param name="verb">The HTTP verb of the route you wish to configure.</param>
        /// <param name="route">The HTTP route that you wish to configure.</param>
        /// <param name="configuration">A route configuration object.</param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task ConfigureRouteAsync(HttpMethod verb, string route, RouteConfiguration configuration)
        {
            route = route.TrimStart('/');
            SetServicePointManagerSecurityProtocol();
            var requestUri = $"{_stubBaseUrl}/Stub/{verb}/api/{route}";
            var jsonConfiguration = JsonConvert.SerializeObject(configuration);
            var response =
                await _httpClient.PostAsync(requestUri, new StringContent(jsonConfiguration));

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Response from: {requestUri}. Response status code does not indicate success {(int)response.StatusCode}:{response.StatusCode}");
            }
        }

        /// <summary>
        ///     Reset all of the stub configuration.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task Reset()
        {
            SetServicePointManagerSecurityProtocol();
            var requestUri = $"{_stubBaseUrl}/stub";
            var response = await _httpClient.DeleteAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Response from: {requestUri}. Response status code does not indicate success {(int)response.StatusCode}:{response.StatusCode}");
            }
        }

        /// <summary>
        ///     Checks to see if the stub is running.
        /// </summary>
        /// <returns>Returns true if the stub is running. Otherwise it returns false.</returns>
        public async Task<bool> HealthCheck()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_stubBaseUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<Option<T>> GetValueFromStub<T>(string requestUri)
        {
            var response = await _httpClient.GetAsync(requestUri);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return Option<T>.None;

            var result = JsonConvert.DeserializeObject<T>(responseBody);
            return new Option<T>(result);
        }

        private void SetServicePointManagerSecurityProtocol()
        {
            //Appears to get cleared out seemingly arbitrarily- calling ths before each http _httpClient is created.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
        }

        /// <summary>
        ///     Dispose method for the stub client.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}