/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.IO;
using System.Net.Http;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Api;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Requests;
using ILogger = SonarQube.Client.Logging.ILogger;

namespace SonarQube.Client;

public class SonarQubeService : ISonarQubeService, IDisposable
{
    private const string MinSqVersionSupportingBearer = "10.4";
    private readonly IHttpClientHandlerFactory httpClientHandlerFactory;
    private readonly ILogger logger;
    private readonly ILanguageProvider languageProvider;
    private readonly IRequestFactorySelector requestFactorySelector;
    private readonly ISSEStreamReaderFactory sseStreamReaderFactory;
    private readonly string userAgent;
    private HttpClient currentHttpClient;
    private ServerInfo currentServerInfo;
    private IRequestFactory requestFactory;

    public SonarQubeService(string userAgent, ILogger logger, ILanguageProvider languageProvider)
        : this(new HttpClientHandlerFactory(new ProxyDetector(), logger), userAgent, logger, languageProvider, new RequestFactorySelector(), new SSEStreamReaderFactory(logger))
    {
    }

    internal /* for testing */ SonarQubeService(
        IHttpClientHandlerFactory httpClientHandlerFactory,
        string userAgent,
        ILogger logger,
        ILanguageProvider languageProvider,
        IRequestFactorySelector requestFactorySelector,
        ISSEStreamReaderFactory sseStreamReaderFactory)
    {
        this.httpClientHandlerFactory = httpClientHandlerFactory ?? throw new ArgumentNullException(nameof(httpClientHandlerFactory));
        this.userAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.languageProvider = languageProvider ?? throw new ArgumentNullException(nameof(languageProvider));

        this.requestFactorySelector = requestFactorySelector;
        this.sseStreamReaderFactory = sseStreamReaderFactory;
    }

    public bool IsConnected => GetServerInfo() != null;

    public ServerInfo GetServerInfo() => currentServerInfo;

    public async Task ConnectAsync(ConnectionInformation connection, CancellationToken token)
    {
        logger.Info($"Connecting to '{connection.ServerUri}'.");
        logger.Debug($"IsConnected is {IsConnected}.");

        requestFactory = requestFactorySelector.Select(connection.IsSonarCloud, logger);

        try
        {
            var serverTypeDescription = connection.IsSonarCloud ? "SonarCloud" : "SonarQube";

            logger.Debug($"Getting the version of {serverTypeDescription}...");
            var serverInfo = await GetServerInfo(connection, token);

            logger.Info($"Connected to {serverTypeDescription} '{serverInfo.Version}'.");
            currentHttpClient = CreateHttpClient(connection.ServerUri, connection.Credentials, ShouldUseBearer(serverInfo));

            logger.Debug("Validating the credentials...");
            var credentialResponse = await InvokeUncheckedRequestAsync<IValidateCredentialsRequest, bool>(request => { }, token);
            if (!credentialResponse)
            {
                throw new InvalidOperationException("Invalid credentials");
            }

            logger.Debug("Credentials accepted.");
            currentServerInfo = serverInfo;
        }
        catch
        {
            currentServerInfo = null;
            throw;
        }
    }

    public void Disconnect()
    {
        logger.Info("Disconnecting...");
        logger.Debug($"Current state before disconnecting is '{(IsConnected ? "Connected" : "Disconnected")}'.");

        // Don't dispose the HttpClient when disconnecting. We'll need it if
        // the caller connects to another server.
        currentServerInfo = null;
        requestFactory = null;
    }

    public async Task<IList<SonarQubeNotification>> GetNotificationEventsAsync(
        string projectKey,
        DateTimeOffset eventsSince,
        CancellationToken token) =>
        await InvokeCheckedRequestAsync<IGetNotificationsRequest, SonarQubeNotification[]>(
            request =>
            {
                request.ProjectKey = projectKey;
                request.EventsSince = eventsSince;
            },
            token);

    public async Task<IList<string>> SearchFilesByNameAsync(
        string projectKey,
        string branch,
        string fileName,
        CancellationToken token) =>
        await InvokeCheckedRequestAsync<ISearchFilesByNameRequest, string[]>(
            request =>
            {
                request.ProjectKey = projectKey;
                request.BranchName = branch;
                request.FileName = fileName;
            },
            token
        );

    public Uri GetViewIssueUrl(string projectKey, string issueKey)
    {
        EnsureIsConnected();

        // The URL should be in the same form as the permalink generated by SQ/SC e.g
        // SonarQube : http://localhost:9000/project/issues?id=security1&issues=AXZRhxr-9W_phHQ8Bzgn&open=AXZRhxr-9W_phHQ8Bzgn
        // SonarCloud: https://sonarcloud.io/project/issues?id=sonarlint-visualstudio&issues=AW-EuNQbXwT7-YPcXpll&open=AW-EuNQbXwT7-YPcXpll
        // Versioning: so far the format of the URL is the same across all versions from at least v6.7
        const string ViewIssueRelativeUrl = "project/issues?id={0}&issues={1}&open={1}";

        return new Uri(currentHttpClient.BaseAddress, string.Format(ViewIssueRelativeUrl, projectKey, issueKey));
    }

    public async Task<IList<SonarQubeProjectBranch>> GetProjectBranchesAsync(string projectKey, CancellationToken token) =>
        await InvokeCheckedRequestAsync<IGetProjectBranchesRequest, SonarQubeProjectBranch[]>(
            request =>
            {
                request.ProjectKey = projectKey;
            }, token);

    public async Task<ISSEStreamReader> CreateSSEStreamReader(string projectKey, CancellationToken token)
    {
        var networkStream = await InvokeCheckedRequestAsync<IGetSonarLintEventStream, Stream>(
            request =>
            {
                request.ProjectKey = projectKey;
            },
            token);

        return sseStreamReaderFactory.Create(networkStream, token);
    }

    /// <summary>
    ///     Creates a new instance of the specified TRequest request, configures and invokes it and returns its response.
    /// </summary>
    /// <typeparam name="TRequest">The request interface to invoke.</typeparam>
    /// <typeparam name="TResponse">The type of the request response result.</typeparam>
    /// <param name="configure">Action that configures a type instance that implements TRequest.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Returns the result of the request invocation.</returns>
    private Task<TResponse> InvokeCheckedRequestAsync<TRequest, TResponse>(
        Action<TRequest> configure,
        CancellationToken token)
        where TRequest : IRequest<TResponse>
    {
        EnsureIsConnected();

        return InvokeUncheckedRequestAsync<TRequest, TResponse>(configure, token);
    }

    /// <summary>
    ///     Executes the call without checking whether the connection to the server has been established. This should only normally be used directly while connecting.
    ///     Other uses should call <see cref="InvokeCheckedRequestAsync{TRequest,TResponse}(System.Threading.CancellationToken)" />.
    /// </summary>
    private async Task<TResponse> InvokeUncheckedRequestAsync<TRequest, TResponse>(Action<TRequest> configure, CancellationToken token)
        where TRequest : IRequest<TResponse> =>
        await InvokeUncheckedRequestAsync<TRequest, TResponse>(configure, currentHttpClient, token);

    /// <summary>
    ///     Executes the call without checking whether the connection to the server has been established. This should only normally be used directly while connecting.
    ///     Other uses should call <see cref="InvokeCheckedRequestAsync{TRequest,TResponse}(System.Threading.CancellationToken)" />.
    /// </summary>
    protected virtual async Task<TResponse> InvokeUncheckedRequestAsync<TRequest, TResponse>(Action<TRequest> configure, HttpClient httpClient, CancellationToken token)
        where TRequest : IRequest<TResponse>
    {
        var request = requestFactory.Create<TRequest>(currentServerInfo);
        configure(request);

        var result = await request.InvokeAsync(httpClient, token);

        return result;
    }

    protected virtual ServerInfo EnsureIsConnected()
    {
        var serverInfo = GetServerInfo();

        if (serverInfo == null)
        {
            logger.Error("The service is expected to be connected.");
            throw new InvalidOperationException("This operation expects the service to be connected.");
        }

        return serverInfo;
    }

    internal /* for testing */ static string GetOrganizationKeyForWebApiCalls(string organizationKey, ILogger logger)
    {
        // Special fake internal key for testing binding to a large number of organizations.
        // If the special key is used we'll pass null for the organization so no filtering will
        // be done.
        const string FakeInternalTestingOrgKey = "sonar.internal.testing.no.org";

        if (FakeInternalTestingOrgKey.Equals(organizationKey, StringComparison.OrdinalIgnoreCase))
        {
            logger.Debug($"DEBUG: org key is {FakeInternalTestingOrgKey}. Setting it to null.");
            return null;
        }
        return organizationKey;
    }

    private static bool ShouldUseBearer(ServerInfo serverInfo) => serverInfo.ServerType == ServerType.SonarCloud || serverInfo.Version >= Version.Parse(MinSqVersionSupportingBearer);

    private async Task<ServerInfo> GetServerInfo(ConnectionInformation connection, CancellationToken token)
    {
        var http = CreateHttpClient(connection.ServerUri, new NoCredentials(), true);
        var versionResponse = await InvokeUncheckedRequestAsync<IGetVersionRequest, string>(request => { }, http, token);
        var serverInfo = new ServerInfo(Version.Parse(versionResponse), connection.IsSonarCloud ? ServerType.SonarCloud : ServerType.SonarQube);
        return serverInfo;
    }

    private HttpClient CreateHttpClient(Uri baseAddress, IConnectionCredentials credentials, bool shouldUseBearer)
    {
        var handler = httpClientHandlerFactory.Create(baseAddress);
        var client = new HttpClient(handler) { BaseAddress = baseAddress, DefaultRequestHeaders = { Authorization = AuthenticationHeaderFactory.Create(credentials, shouldUseBearer) } };
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);

        return client;
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        logger.Debug("Disposing SonarQubeService...");
        if (!disposedValue)
        {
            logger.Debug("SonarQubeService was not disposed, continuing with dispose...");
            if (disposing)
            {
                currentServerInfo = null;
            }

            disposedValue = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion // IDisposable Support
}
