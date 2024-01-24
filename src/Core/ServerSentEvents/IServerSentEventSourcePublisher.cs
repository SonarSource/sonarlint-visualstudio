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
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.Core.ServerSentEvents
{
    /// <summary>
    /// The publishing side for the <see cref="IServerSentEventSource{T}"/>
    /// </summary>
    /// <remarks>This interface is not intended to be thread safe.
    /// The only permitted type of multi threaded calling is calling Publish and Dispose concurrently, although it may result in <see cref="ObjectDisposedException"/></remarks>
    /// <typeparam name="T">Server sent event type inherited from <see cref="IServerEvent"/></typeparam>
    public interface IServerSentEventSourcePublisher<in T> : IDisposable where T : class, IServerEvent
    {
        /// <summary>
        /// Publishes the event to the consumer channel.
        /// <exception cref="ObjectDisposedException">After the instance has been disposed</exception>.
        /// </summary>
        /// <param name="serverEvent">Server event (<see cref="IServerEvent"/>) that needs to be delivered to the consumer</param>
        /// <returns></returns>
        void Publish(T serverEvent);
    }
}
