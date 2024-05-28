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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.SLCore.Listener.Binding;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation
{
    [Export(typeof(IConnectedModeSuggestionListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ConnectedModeSuggestionListener : IConnectedModeSuggestionListener
    {
        private readonly IBindingSuggestionHandler bindingSuggestionHandler;
        private readonly IActiveConfigScopeTracker activeConfigScopeTracker;

        [ImportingConstructor]
        public ConnectedModeSuggestionListener(IBindingSuggestionHandler bindingSuggestionHandler, IActiveConfigScopeTracker activeConfigScopeTracker)
        {
            this.bindingSuggestionHandler = bindingSuggestionHandler;
            this.activeConfigScopeTracker = activeConfigScopeTracker;
        }

        public Task<AssistCreatingConnectionResponse> AssistCreatingConnectionAsync(AssistCreatingConnectionParams parameters)
        {
            bindingSuggestionHandler.Notify();

            AssistCreatingConnectionResponse result = new(activeConfigScopeTracker.Current.Id);
            return Task.FromResult(result);
        }

        public void NoBindingSuggestionFound(NoBindingSuggestionFoundParams parameters)
        {
            bindingSuggestionHandler.Notify();
        }
    }
}
