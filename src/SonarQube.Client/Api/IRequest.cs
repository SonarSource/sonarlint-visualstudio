using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SonarQube.Client
{
    public interface IRequest
    {
    }

    public interface IRequest<TResponse> : IRequest
    {
        Task<TResponse> InvokeAsync(HttpClient httpClient, CancellationToken token);
    }
}
