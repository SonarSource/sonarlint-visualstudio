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

using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    internal interface IServerSentEventPump
    {
        Task PumpAllAsync(IServerSentEventsSession session);
    }

    [Export(typeof(IServerSentEventPump))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerSentEventPump : IServerSentEventPump
    {
        private readonly IServerSentEventsFilter serverSentEventsFilter;
        private readonly IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher;
        private readonly ITaintServerEventSourcePublisher taintServerEventSourcePublisher;

        [ImportingConstructor]
        public ServerSentEventPump(IServerSentEventsFilter serverSentEventsFilter,
            IIssueChangedServerEventSourcePublisher issueChangedServerEventSourcePublisher,
            ITaintServerEventSourcePublisher taintServerEventSourcePublisher)
        {
            this.serverSentEventsFilter = serverSentEventsFilter;
            this.issueChangedServerEventSourcePublisher = issueChangedServerEventSourcePublisher;
            this.taintServerEventSourcePublisher = taintServerEventSourcePublisher;
        }

        public async Task PumpAllAsync(IServerSentEventsSession session)
        {
            while (true)
            {
                try
                {
                    var filteredServerEvent = serverSentEventsFilter.GetFilteredEventOrNull(await session.ReadAsync());

                    if (filteredServerEvent == null)
                    {
                        continue;
                    }

                    switch (filteredServerEvent)
                    {
                        case IIssueChangedServerEvent issueChangedServerEvent:
                            issueChangedServerEventSourcePublisher.Publish(issueChangedServerEvent);
                            break;
                        case ITaintServerEvent taintServerEvent:
                            taintServerEventSourcePublisher.Publish(taintServerEvent);
                            break;
                    }
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
                {
                    return;
                }
            }
        }
    }
}
