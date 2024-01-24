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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SonarQube.Client.Models.ServerSentEvents.ServerContract
{
    /// <summary>
    /// Reads lines from the network stream and aggregates them into <see cref="ISqServerEvent"/>.
    /// </summary>
    /// <returns>
    /// Returns aggregated <see cref="ISqServerEvent"/> or null if the stream ended or the task was cancelled.
    /// Will throw if there was a problem reading from the underlying stream.
    /// </returns>
    internal interface ISqSSEStreamReader : IDisposable
    {
        Task<ISqServerEvent> ReadAsync();
    }

    /// <summary>
    /// Code on the java side: https://github.com/SonarSource/sonarlint-core/blob/171ca4d75c24033e115a81bd7481427cd1f39f4c/server-api/src/main/java/org/sonarsource/sonarlint/core/serverapi/stream/EventBuffer.java
    /// </summary>
    internal sealed class SqSSEStreamReader : ISqSSEStreamReader
    {
        private readonly StreamReader networkStreamReader;
        private readonly CancellationToken cancellationToken;
        private readonly ISqServerSentEventParser sqServerSentEventParser;

        public SqSSEStreamReader(StreamReader networkStreamReader, CancellationToken cancellationToken)
            : this(networkStreamReader, cancellationToken, new SqServerSentEventParser())
        {
        }

        internal SqSSEStreamReader(StreamReader networkStreamReader,
            CancellationToken cancellationToken,
            ISqServerSentEventParser sqServerSentEventParser)
        {
            this.networkStreamReader = networkStreamReader;
            this.cancellationToken = cancellationToken;
            this.sqServerSentEventParser = sqServerSentEventParser;
        }

        public async Task<ISqServerEvent> ReadAsync()
        {
            var eventLines = new List<string>();

            while (!networkStreamReader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await networkStreamReader.ReadLineAsync();
                var isEventEnd = string.IsNullOrEmpty(line);

                if (isEventEnd)
                {
                    var parsedEvent = sqServerSentEventParser.Parse(eventLines);

                    eventLines.Clear();

                    if (parsedEvent != null)
                    {
                        return parsedEvent;
                    }
                }
                else
                {
                    eventLines.Add(line);
                }
            }

            return null;
        }

        public void Dispose()
        {
            networkStreamReader.Dispose();
        }
    }
}
