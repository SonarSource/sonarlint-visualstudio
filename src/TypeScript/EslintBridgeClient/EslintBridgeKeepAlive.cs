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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    /// <summary>
    /// Component that is responsible for sending keep-alive messages
    /// to the eslint-bridge
    /// </summary>
    internal interface IEslintBridgeKeepAlive : IDisposable
    {
    }

    internal sealed class EslintBridgeKeepAlive : IEslintBridgeKeepAlive
    {
        // The eslintbridge process will shutdown after 15 seconds.
        // We send a keep alive every 5 seconds.
        private const double MillisecondsToWaitBetweenKeepAlives = 5000;

        private readonly IEslintBridgeProcess process;
        private readonly IEslintBridgeHttpWrapper httpWrapper;
        private readonly ITimer timer;
        private readonly ILogger logger;

        public EslintBridgeKeepAlive(IEslintBridgeProcess process, ILogger logger)
            : this(process, logger, new EslintBridgeHttpWrapper(logger), new TimerWrapper())
        {
        }

        internal /* for testing */ EslintBridgeKeepAlive(IEslintBridgeProcess process, ILogger logger,
            IEslintBridgeHttpWrapper httpWrapper, ITimer timer)
        {
            this.process = process;
            this.logger = logger;
            this.httpWrapper = httpWrapper;

            this.timer = timer;
            this.timer.AutoReset = true;
            this.timer.Interval = MillisecondsToWaitBetweenKeepAlives;
            this.timer.Elapsed += OnKeepAliveTimerElapsed;

            // Note: we're starting the timer immediately, even though the process might not be running
            // yet (i.e. the user hasn't tried to analyse a JS/TS file)
            this.timer.Start();
        }

        private void OnKeepAliveTimerElapsed(object sender, TimerEventArgs e)
            => HandleKeepAliveTimerElapsedAsync().Forget();

        internal /* for testing */async Task HandleKeepAliveTimerElapsedAsync()
        {
            try
            {
                // Stopping the timer here means we won't send multiple keep-alives
                // if the server is busy analysing and doesn't respond to the first
                // call. It also makes debugging this method simpler.
                timer.Stop();
                if (process.IsRunning)
                {
                    await SendKeepAliveAsync();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose($"[EsLintBridgeClient] Error sending keep-alive: {ex}");
            }
            finally
            {
                timer.Start();
            }
        }

        private async Task SendKeepAliveAsync()
        {
            var port = await process.Start();
            var fullServerUrl = BuildServerUri(port, "status");
            await httpWrapper.GetAsync(fullServerUrl, CancellationToken.None);
        }

        private Uri BuildServerUri(int port, string endpoint) => new Uri($"http://localhost:{port}/{endpoint}");

        public void Dispose() => timer.Dispose();
    }
}
