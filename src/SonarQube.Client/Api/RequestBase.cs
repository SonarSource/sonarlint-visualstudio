using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Helpers;
using SonarQube.Client.Services;

namespace SonarQube.Client
{
    public abstract class RequestBase<TResponse> : IRequest<TResponse>
    {
        [JsonIgnore]
        protected abstract string Path { get; }

        [JsonIgnore]
        protected virtual HttpMethod HttpMethod => HttpMethod.Get;

        protected abstract TResponse ParseResponse(string response);

        public virtual async Task<TResponse> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = await InvokeImplAsync(httpClient, token);

            result.EnsureSuccess();

            return result.Value;
        }

        protected virtual async Task<Result<TResponse>> InvokeImplAsync(HttpClient httpClient, CancellationToken token)
        {
            string query = QueryStringSerializer.ToQueryString(this);

            var pathAndQuery = string.IsNullOrEmpty(query) ? Path : $"{Path}?{query}";

            var httpRequest = new HttpRequestMessage(HttpMethod, new Uri(pathAndQuery, UriKind.Relative));

            var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);

            return await ReadResponse(httpResponse);
        }

        protected virtual async Task<Result<TResponse>> ReadResponse(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                return new Result<TResponse>(httpResponse, default(TResponse));
            }

            var responseString = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return new Result<TResponse>(httpResponse, ParseResponse(responseString));
        }
    }
}
