using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Services;

namespace SonarQube.Client.Api
{
    public abstract class PagedRequestBase<TResponseItem> : RequestBase<TResponseItem[]>, IPagedRequest<TResponseItem>
    {
        private const int FirstPage = 1;
        private const int MaximumPageSize = 500;

        [JsonProperty("p")]
        public int Page { get; set; } = FirstPage;

        [JsonProperty("ps")]
        public int PageSize { get; set; } = MaximumPageSize;

        public async override Task<TResponseItem[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = new List<TResponseItem>();

            Result<TResponseItem[]> pageResult;
            do
            {
                pageResult = await InvokeImplAsync(httpClient, token);
                pageResult.EnsureSuccess();

                result.AddRange(pageResult.Value);

                Page++;
            }
            while (pageResult.Value.Length >= MaximumPageSize);

            return result.ToArray();
        }
    }
}
