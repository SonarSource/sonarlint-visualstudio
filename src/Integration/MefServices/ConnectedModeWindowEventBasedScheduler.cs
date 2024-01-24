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
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    internal interface IConnectedModeWindowEventListener: IDisposable
    {
        void SubscribeToConnectedModeWindowEvents(IHost connectedModeWindowHost);
    }
    
    internal interface IConnectedModeWindowEventBasedScheduler
    {
        void ScheduleActionOnNextEvent(Action action);
    }

    [Export(typeof(IConnectedModeWindowEventBasedScheduler))]
    [Export(typeof(IConnectedModeWindowEventListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ConnectedModeWindowEventBasedScheduler : IConnectedModeWindowEventBasedScheduler, IConnectedModeWindowEventListener
    {
        private bool disposed = false;
        
        private IHost host;
        private Action nextScheduledAction;

        public void SubscribeToConnectedModeWindowEvents(IHost connectedModeWindowHost)
        {
            if (host != null)
            {
                throw new ArgumentException(nameof(host));
            }
            
            host = connectedModeWindowHost;
            host.ActiveSectionChanged += ActiveSectionChangedListener;
        }
        
        public void ScheduleActionOnNextEvent(Action action)
        {
            nextScheduledAction = action;
        }
        
        internal /* for testing */ void ActiveSectionChangedListener(object sender, EventArgs args)
        {
            if (nextScheduledAction == null)
            {
                return;
            }

            HandlePostponedAutobind();
        }

        private void HandlePostponedAutobind()
        {
            nextScheduledAction();
            nextScheduledAction = null;
        }
        
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            host.ActiveSectionChanged -= ActiveSectionChangedListener;
        }
    }
}
