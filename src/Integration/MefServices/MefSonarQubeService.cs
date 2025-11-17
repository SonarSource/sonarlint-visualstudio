/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.ComponentModel.Composition;
using System.Net.Http;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ETW;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.MefServices;

/// <summary>
/// This class exists for a couple of reasons:
/// * to add VS-specific thread handling and ETW tracing
/// * to avoid bringing MEF composition to the SonarQube.Client assembly which
///   can be used in contexts where it is not required.
/// </summary>
[Export(typeof(ISonarQubeService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public sealed class MefSonarQubeService(IUserAgentProvider userAgentProvider, ILogger logger, ILanguageProvider languageProvider, IThreadHandling threadHandling)
    : SonarQubeService(
        userAgent: userAgentProvider.UserAgent,
        logger: new LoggerAdapter(logger),
        languageProvider: languageProvider)
{
    protected override async Task<TResponse> InvokeUncheckedRequestAsync<TRequest, TResponse>(Action<TRequest> configure, HttpClient httpClient, CancellationToken token)
    {
        CodeMarkers.Instance.WebClientCallStart(typeof(TRequest).Name);

        var result = await threadHandling.RunOnBackgroundThread(() => base.InvokeUncheckedRequestAsync<TRequest, TResponse>(configure, httpClient, token));

        CodeMarkers.Instance.WebClientCallStop(typeof(TRequest).Name);

        return result;
    }

    private sealed class LoggerAdapter(ILogger logger) : SonarQube.Client.Logging.ILogger
    {
        public void Debug(string message) =>
            // This will only be logged if an env var is set
            logger.LogVerbose(message);

        public void Error(string message) => logger.WriteLine($"ERROR: {message}");

        public void Info(string message) => logger.WriteLine($"{message}");

        public void Warning(string message) => logger.WriteLine($"WARNING: {message}");
    }
}
