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

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.ServerSentEvents
{
    /// <summary>
    /// Reacts to project changes and opens/closes Server Sent Events sessions
    /// </summary>
    internal interface ISSESessionManager : IDisposable
    {
    }

    [Export(typeof(ISSESessionManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SSESessionManager : ISSESessionManager
    {
        private readonly object syncRoot = new object();
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ISSESessionFactory sseSessionFactory;

        private ISSESession currentSession;

        private bool disposed;

        [ImportingConstructor]
        public SSESessionManager(IActiveSolutionBoundTracker activeSolutionBoundTracker, ISSESessionFactory sseSessionFactory)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.sseSessionFactory = sseSessionFactory;

            activeSolutionBoundTracker.SolutionBindingChanged += SolutionBindingChanged;

            CreateSessionIfInConnectedMode(activeSolutionBoundTracker.CurrentConfiguration);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            activeSolutionBoundTracker.SolutionBindingChanged -= SolutionBindingChanged;
            sseSessionFactory.Dispose();
            EndCurrentSession();
            disposed = true;
        }

        private void SolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs activeSolutionBindingEventArgs)
        {
            CreateSessionIfInConnectedMode(activeSolutionBindingEventArgs.Configuration);
        }

        private void CreateSessionIfInConnectedMode(BindingConfiguration bindingConfiguration)
        {
            ISSESession sessionToLaunch;
            lock (syncRoot)
            {
                EndCurrentSession();
                
                var isInConnectedMode = !bindingConfiguration.Equals(BindingConfiguration.Standalone);

                if (!isInConnectedMode)
                {
                    return;
                }

                currentSession = sessionToLaunch = sseSessionFactory.Create(bindingConfiguration.Project.ProjectKey);
            }

            sessionToLaunch.PumpAllAsync().Forget();
        }

        private void EndCurrentSession()
        {
            lock (syncRoot)
            {
                currentSession?.Dispose();
                currentSession = null;
            }
        }
    }
}
