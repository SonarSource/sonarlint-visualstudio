using System.Net.Http;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Http.Helper;

internal record AnalysisRequestConfig(SecureString Token, string RequestUri, params string[] FileNames);

internal sealed class HttpRequester : IDisposable
{
    private const int WaitForServerMsTimeout = 2000;
    private const string JsonMediaType = "application/json";
    private const string XAuthTokenHeader = "X-Auth-Token";
    private readonly HttpClient httpClient;

    public HttpRequester(int requestTimeout = WaitForServerMsTimeout)
    {
        httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMilliseconds(requestTimeout);
    }

    public void Dispose() => httpClient.Dispose();

    internal async Task<HttpResponseMessage> SendRequest(AnalysisRequestConfig analysisRequestConfig)
    {
        var fileNames = analysisRequestConfig.FileNames.Select(x => new FileUri(x));
        var analysisRequest = new AnalysisRequest { FileNames = [.. fileNames] };
        var body = JsonConvert.SerializeObject(analysisRequest);

        return await SendRequest(analysisRequestConfig.Token.ToUnsecureString(), analysisRequestConfig.RequestUri, body);
    }

    internal async Task<HttpResponseMessage> SendRequest(string token, string requestUri, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add(XAuthTokenHeader, token);
        request.Content = new StringContent(body, Encoding.UTF8, JsonMediaType);

        var response = await httpClient.SendAsync(request);
        return response;
    }
}
