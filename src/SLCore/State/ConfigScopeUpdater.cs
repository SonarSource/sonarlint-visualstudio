/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;

namespace SonarLint.VisualStudio.SLCore.State;

[Export(typeof(IConfigScopeUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ConfigScopeUpdater(
    IActiveConfigScopeTracker activeConfigScopeTracker,
    ISolutionInfoProvider solutionInfoProvider,
    Lazy<ISLCoreHandler> slCoreHandler,
    IThreadHandling threadHandling)
    : IConfigScopeUpdater
{
    public void UpdateConfigScopeForCurrentSolution(BoundServerProject currentBinding)
    {
        var solutionName = solutionInfoProvider.GetSolutionName();

        threadHandling.RunOnBackgroundThread(() =>
        {
            HandleConfigScopeUpdateInternal(solutionName,
                currentBinding?.ServerConnection.Id,
                currentBinding?.ServerProjectKey);
            return Task.FromResult(0);
        }).Forget();
    }

    private void HandleConfigScopeUpdateInternal(string solutionName, string connectionId, string projectKey)
    {
        try
        {
            if (solutionName is null)
            {
                activeConfigScopeTracker.RemoveCurrentConfigScope();
            }
            else
            {
                activeConfigScopeTracker.SetCurrentConfigScope(solutionName, connectionId, projectKey);
            }
        }
        catch (InvalidOperationException e) when(e.Message == SLCoreStrings.ServiceProviderNotInitialized)
        {
            slCoreHandler.Value.ShowNotificationIfNeeded();
        }
    }
}
