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

using System;
using System.IO;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models.ServerSentEvents.ServerContract;

namespace SonarQube.Client.Models.ServerSentEvents
{
    public interface ISSEStreamWriter : IDisposable
    {
        /// <summary>
        /// Begin pumping events. Will block the calling thread with an infinite loop.
        /// </summary>
        Task BeginListening();
    }

    internal class SSEStreamWriter : ISSEStreamWriter
    {
        private readonly Stream serverStream;
        private readonly ChannelWriter<ISqServerEvent> sqEventsChannel;
        private readonly CancellationToken cancellationToken;
        private readonly ISqServerSentEventParser sqServerSentEventParser;

        public SSEStreamWriter(Stream serverStream,
            ChannelWriter<ISqServerEvent> sqEventsChannel,
            CancellationToken cancellationToken)
            : this(serverStream, sqEventsChannel, cancellationToken, new SqServerSentEventParser())
        {
        }

        internal SSEStreamWriter(Stream serverStream,
            ChannelWriter<ISqServerEvent> sqEventsChannel,
            CancellationToken cancellationToken,
            ISqServerSentEventParser sqServerSentEventParser)
        {
            this.serverStream = serverStream;
            this.cancellationToken = cancellationToken;
            this.sqEventsChannel = sqEventsChannel;
            this.sqServerSentEventParser = sqServerSentEventParser;
        }

        public Task BeginListening()
        {
            // todo: next PR
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            sqEventsChannel.Complete();
        }
    }
}
