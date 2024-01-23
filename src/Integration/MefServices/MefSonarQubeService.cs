/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ETW;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Service;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    /// <summary>
    /// This class exists for a couple of reasons:
    /// * to add VS-specific thread handling and ETW tracing
    /// * to avoid bringing MEF composition to the SonarQube.Client assembly which 
    ///   can be used in contexts where it is not required.
    /// </summary>
    [Export(typeof(ISonarQubeService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class MefSonarQubeService : SonarQubeService
    {
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public MefSonarQubeService(ILogger logger)
            : this(logger, ThreadHandling.Instance)
        {
        }

        internal /* for testing */ MefSonarQubeService(ILogger logger, IThreadHandling threadHandling)
            : base(new HttpClientHandler(),
                userAgent: $"SonarLint Visual Studio/{VersionHelper.SonarLintVersion}",
                logger: new LoggerAdapter(logger))
        {
            this.threadHandling = threadHandling;
        }

        protected override async Task<TResponse> InvokeUncheckedRequestAsync<TRequest, TResponse>(Action<TRequest> configure, CancellationToken token)
        {
            CodeMarkers.Instance.WebClientCallStart(typeof(TRequest).Name);

            Func<Task<TResponse>> asyncMethod = () => base.InvokeUncheckedRequestAsync<TRequest, TResponse>(configure, token);

            var result = await threadHandling.RunOnBackgroundThread(asyncMethod);

            CodeMarkers.Instance.WebClientCallStop(typeof(TRequest).Name);

            return result;
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
                logger.LogVerbose(message);

            public void Error(string message) =>
                logger.WriteLine($"ERROR: {message}");

            public void Info(string message) =>
                logger.WriteLine($"{message}");

            public void Warning(string message) =>
                logger.WriteLine($"WARNING: {message}");
        }
    }
}
