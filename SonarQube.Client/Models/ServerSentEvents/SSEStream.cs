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

using System.Threading.Tasks;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarQube.Client.Models.ServerSentEvents
{
    /// <summary>
    /// Wraps the stream response from the server, reads from it and converts it to <see cref="IServerEvent"/>
    /// </summary>
    /// <remarks>
    /// Aggregate interface for SLVS.
    /// SLVS should be the one to decide on which thread <see cref="ISSEStreamWriter.BeginListening"/> and
    /// <see cref="ISSEStreamReader.ReadAsync"/> will run.
    /// </remarks>
    public interface ISSEStream : ISSEStreamReader, ISSEStreamWriter
    {
    }

    internal sealed class SSEStream : ISSEStream
    {
        private readonly ISSEStreamReader streamReader;
        private readonly ISSEStreamWriter streamWriter;

        public SSEStream(ISSEStreamReader streamReader, ISSEStreamWriter streamWriter)
        {
            this.streamReader = streamReader;
            this.streamWriter = streamWriter;
        }

        public Task BeginListening()
        {
            return streamWriter.BeginListening();
        }

        public Task<IServerEvent> ReadAsync()
        {
            return streamReader.ReadAsync();
        }

        public void Dispose()
        {
            streamWriter.Dispose();
        }
    }
}
