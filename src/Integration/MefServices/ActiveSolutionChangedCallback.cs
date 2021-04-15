/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    /// <summary>
    /// Listens to <see cref="IActiveSolutionTracker.ActiveSolutionChanged"/> and calls the imported callbacks
    /// </summary>
    public interface IActiveSolutionChangedCallback : IDisposable
    {
    }

    [Export(typeof(IActiveSolutionChangedCallback))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionChangedCallback : IActiveSolutionChangedCallback
    {
        internal const string CallbackContractName = "ActiveSolutionChangedCallback";

        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IEnumerable<Action> callbacks;

        [ImportingConstructor]
        public ActiveSolutionChangedCallback(
            IActiveSolutionTracker activeSolutionTracker,
            [ImportMany(CallbackContractName)] IEnumerable<Action> callbacks)
        {
            this.activeSolutionTracker = activeSolutionTracker;
            this.callbacks = callbacks;

            activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;
        }

        public void Dispose()
        {
            activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTracker_ActiveSolutionChanged;
        }

        private void ActiveSolutionTracker_ActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            foreach (var callback in callbacks)
            {
                callback();
            }
        }
    }
}
