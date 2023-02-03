/*
 * SonarQube Client
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading;
using SonarQube.Client.Models.ServerSentEvents.ServerContract;
using System.Threading.Channels;
using SonarQube.Client.Logging;

namespace SonarQube.Client.Models.ServerSentEvents
{
    internal interface ISSEStreamFactory
    {
        ISSEStream Create(Stream networkStream, CancellationToken cancellationToken);
    }

    internal class SSEStreamFactory : ISSEStreamFactory
    {
        private readonly ILogger logger;

        public SSEStreamFactory(ILogger logger)
        {
            this.logger = logger;
        }

        public ISSEStream Create(Stream networkStream, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<ISqServerEvent>();

            var reader = new SSEStreamReader(channel.Reader, cancellationToken, logger);
            var writer = new SSEStreamWriter(new StreamReader(networkStream), channel.Writer, cancellationToken);

            return new SSEStream(reader, writer);
        }
    }
}
