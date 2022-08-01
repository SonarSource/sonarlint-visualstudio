/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Service;
using SonarQube.Client;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    /// <summary>
    /// Decorator: adds logging and threading checks to the core implementation.
    /// It also means we can avoid bringing MEF composition to the SonarQube.Client assembly.
    /// </summary>
    [Export(typeof(ISonarQubeService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [ExcludeFromCodeCoverage]
    public class MefSonarQubeService : ISonarQubeService
    {
        private readonly ISonarQubeService wrapped;
        private readonly ILogger logger;

        [ImportingConstructor]
        public MefSonarQubeService(ILogger logger)
        {
            wrapped = new SonarQubeService(new HttpClientHandler(),
                userAgent: $"SonarLint Visual Studio/{VersionHelper.SonarLintVersion}",
                logger: new LoggerAdapter(logger));

            this.logger = logger;
        }

        ServerInfo ISonarQubeService.ServerInfo => wrapped.ServerInfo;

        bool ISonarQubeService.IsConnected => wrapped.IsConnected;

        async Task ISonarQubeService.ConnectAsync(ConnectionInformation connection, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                await wrapped.ConnectAsync(connection, token);
            }
        }

        void ISonarQubeService.Disconnect()
        {
            // We don't need a ServerCallScope here since we're not making a
            // web call, but it's still useful to logged when it is called.
            var text = $"[ServerCall: Thread: {Thread.CurrentThread.ManagedThreadId}, {DateTime.Now:hh:mm:ss.fff}] {nameof(ISonarQubeService.Disconnect)} called";
            logger.WriteLine(text);

            wrapped.Disconnect();
        }

        Task<Stream> ISonarQubeService.DownloadStaticFileAsync(string pluginKey, string fileName, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.DownloadStaticFileAsync(pluginKey, fileName, token);
            }
        }

        Task<IList<SonarQubeLanguage>> ISonarQubeService.GetAllLanguagesAsync(CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetAllLanguagesAsync(token);
            }
        }

        Task<IList<SonarQubeModule>> ISonarQubeService.GetAllModulesAsync(string projectKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetAllModulesAsync(projectKey, token);
            }
        }

        Task<IList<SonarQubeOrganization>> ISonarQubeService.GetAllOrganizationsAsync(CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetAllOrganizationsAsync(token);
            }
        }

        Task<IList<SonarQubePlugin>> ISonarQubeService.GetAllPluginsAsync(CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetAllPluginsAsync(token);
            }
        }

        Task<IList<SonarQubeProject>> ISonarQubeService.GetAllProjectsAsync(string organizationKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetAllProjectsAsync(organizationKey, token);
            }
        }

        Task<IList<SonarQubeProperty>> ISonarQubeService.GetAllPropertiesAsync(string projectKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetAllPropertiesAsync(projectKey, token);
            }
        }

        Task<SonarQubeHotspot> ISonarQubeService.GetHotspotAsync(string hotspotKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetHotspotAsync(hotspotKey, token);
            }
        }

        Task<IList<SonarQubeNotification>> ISonarQubeService.GetNotificationEventsAsync(string projectKey, DateTimeOffset eventsSince, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetNotificationEventsAsync(projectKey, eventsSince, token);
            }
        }

        Task<IList<SonarQubeProjectBranch>> ISonarQubeService.GetProjectBranchesAsync(string projectKey, CancellationToken cancellation)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetProjectBranchesAsync(projectKey, cancellation);
            }
        }

        Uri ISonarQubeService.GetProjectDashboardUrl(string projectKey)
            => wrapped.GetProjectDashboardUrl(projectKey);

        Task<SonarQubeQualityProfile> ISonarQubeService.GetQualityProfileAsync(string projectKey, string organizationKey, SonarQubeLanguage language, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetQualityProfileAsync(projectKey, organizationKey, language, token);
            }
        }

        Task<RoslynExportProfileResponse> ISonarQubeService.GetRoslynExportProfileAsync(string qualityProfileName, string organizationKey, SonarQubeLanguage language, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetRoslynExportProfileAsync(qualityProfileName, organizationKey, language, token);
            }
        }

        Task<IList<SonarQubeRule>> ISonarQubeService.GetRulesAsync(bool isActive, string qualityProfileKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetRulesAsync(isActive, qualityProfileKey, token);
            }
        }

        Task<ServerExclusions> ISonarQubeService.GetServerExclusions(string projectKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetServerExclusions(projectKey, token);
            }
        }

        Task<string> ISonarQubeService.GetSourceCodeAsync(string fileKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetSourceCodeAsync(fileKey, token);
            }
        }

        Task<IList<SonarQubeIssue>> ISonarQubeService.GetSuppressedIssuesAsync(string projectKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetSuppressedIssuesAsync(projectKey, token);
            }
        }

        Task<IList<SonarQubeIssue>> ISonarQubeService.GetTaintVulnerabilitiesAsync(string projectKey, CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.GetTaintVulnerabilitiesAsync(projectKey, token);
            }
        }

        Uri ISonarQubeService.GetViewHotspotUrl(string projectKey, string hotspotKey)
            => wrapped.GetViewHotspotUrl(projectKey, hotspotKey);

        Uri ISonarQubeService.GetViewIssueUrl(string projectKey, string issueKey)
            => wrapped.GetViewIssueUrl(projectKey, issueKey);

        Task<bool> ISonarQubeService.HasOrganizations(CancellationToken token)
        {
            using (new ServerCallScope(logger))
            {
                return wrapped.HasOrganizations(token);
            }
        }

        private sealed class LoggerAdapter : SonarQube.Client.Logging.ILogger
        {
            private readonly ILogger logger;

            public LoggerAdapter(ILogger logger)
            {
                this.logger = logger;
            }

            public void Debug(string message) =>
                // This will only be logged if an env var is set
                logger.LogDebug(message);

            public void Error(string message) =>
                logger.WriteLine($"ERROR: {message}");

            public void Info(string message) =>
                logger.WriteLine($"{message}");

            public void Warning(string message) =>
                logger.WriteLine($"WARNING: {message}");
        }

        /// <summary>
        /// Scope class to wrap calls to the SonarQube/SonarCloud server.
        /// Adds logging with timing and threading info.
        /// </summary>
        private sealed class ServerCallScope : IDisposable
        {
            private readonly Stopwatch timer;
            private readonly ILogger logger;
            private readonly string callerMemberName;
            private bool disposed;

            public ServerCallScope(ILogger logger, [CallerMemberName] string callerMemberName = null)
            {
                this.logger = logger;
                this.callerMemberName = callerMemberName;

                var text = $"[ServerCall: Thread: {Thread.CurrentThread.ManagedThreadId}, {DateTime.Now:hh:mm:ss.fff}] {callerMemberName} started";
                logger.WriteLine(text);

                timer = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (disposed)
                {
                    Debug.Fail("Only expecting to be disposed once");
                    return;
                };

                disposed = true;

                timer.Stop();

                var text = $"[ServerCall: Thread: {Thread.CurrentThread.ManagedThreadId}, {DateTime.Now:hh:mm:ss.fff}] {callerMemberName} ended. Elapsed: {timer.ElapsedMilliseconds} ms";
                logger.WriteLine(text);
            }
        }
    }
}
