/*
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

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.QualityProfile
{
    /// <summary>
    /// Event listener for <see cref="IQualityProfileServerEventSource"/>
    /// </summary>
    internal interface IQualityProfileServerEventsListener
    {
        Task ListenAsync();
    }

    [Export(typeof(IQualityProfileServerEventsListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class QualityProfileServerEventsListener : IQualityProfileServerEventsListener
    {
        private readonly IQualityProfileServerEventSource eventSource;
        private readonly IQualityProfileUpdater updater;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public QualityProfileServerEventsListener(IQualityProfileServerEventSource eventSource, IQualityProfileUpdater updater, IThreadHandling threadHandling)
        {
            this.eventSource = eventSource;
            this.updater = updater;
            this.threadHandling = threadHandling;
        }

        public async Task ListenAsync()
        {
            await threadHandling.SwitchToBackgroundThread();

            // when event source is disposed, it returns null
            while (await eventSource.GetNextEventOrNullAsync() != null)
            {
                await updater.UpdateAsync();
            }
        }
    }
}
