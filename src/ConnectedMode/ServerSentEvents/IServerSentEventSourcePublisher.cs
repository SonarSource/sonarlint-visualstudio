﻿/*
 * SonarLint for Visual Studio
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

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    /// <summary>
    /// The publishing side for the <see cref="IServerSentEventSource{T}"/>
    /// </summary>
    /// <typeparam name="T">Server sent event type inherited from <see cref="IServerSentEvent"/></typeparam>
    internal interface IServerSentEventSourcePublisher<T> where T : IServerEvent
    {
        /// <summary>
        /// Publishes the event to the consumer channel.
        /// Does not throw.
        /// </summary>
        /// <param name="serverEvent">Server event (<see cref="IServerSentEvent"/>) that needs to be delivered to the consumer</param>
        /// <returns></returns>
        Task PublishAsync(T serverEvent);
    }
}
