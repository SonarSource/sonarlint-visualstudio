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
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.TEMP;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    /// <summary>
    /// Source for the server sent events about certain server side changes to the SQ/SC project
    /// </summary>
    /// <typeparam name="T">Server sent event type inherited from <see cref="IServerSentEvent"/></typeparam>
    internal interface IServerSentEventSource<T> where T : IServerSentEvent
    {
        /// <summary>
        /// Method that is used to await for the next server sent event <see cref="IServerSentEvent"/>.
        /// Does not throw.
        /// </summary>
        /// <returns>
        /// Task which result is:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>Next server event in queue</description>
        ///         </item>
        ///         <item>
        ///             <description>Or null, when the channel has been shut down</description>
        ///         </item>
        ///     </list>
        /// </returns>
        Task<T> GetNextEventOrNullAsync();
    }
}
