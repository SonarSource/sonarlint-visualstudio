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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.SLCore.Core.Process;

internal interface ISLCoreErrorLoggerFactory
{
    ISLCoreErrorLogger Create(StreamReader errorStream);
}

internal interface ISLCoreErrorLogger : IDisposable
{
}

[Export(typeof(ISLCoreErrorLoggerFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SLCoreErrorLoggerFactory : ISLCoreErrorLoggerFactory
{
    private readonly ILogger logger;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public SLCoreErrorLoggerFactory(ILogger logger, IThreadHandling threadHandling)
    {
        this.logger = logger;
        this.threadHandling = threadHandling;
    }

    public ISLCoreErrorLogger Create(StreamReader errorStream)
    {
        return new ErrorLogger(logger, threadHandling, errorStream, new CancellationTokenSource());
    }

    private sealed class ErrorLogger : ISLCoreErrorLogger
    {
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private readonly CancellationTokenSource cancellationTokenSource;

        public ErrorLogger(ILogger logger, IThreadHandling threadHandling, StreamReader streamReader, CancellationTokenSource cancellationTokenSource)
        {
            this.logger = logger;
            this.threadHandling = threadHandling;
            this.cancellationTokenSource = cancellationTokenSource;
            ReadErrorStreamInBackground(streamReader);
        }

        private void ReadErrorStreamInBackground(StreamReader errorStream)
        {
            threadHandling.RunOnBackgroundThread(async () =>
            {
                await ReadAsync(errorStream);
                return 0;
            }).Forget();
        }

        private async Task ReadAsync(StreamReader errorStream)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var line = await errorStream.ReadLineAsync();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                logger.WriteLine("[SLCORE-ERR] " + line);
            }
        }

        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}
