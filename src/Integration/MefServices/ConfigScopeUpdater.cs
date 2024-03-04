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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.Integration;

internal interface IConfigScopeUpdater
{
    void UpdateConfigScopeForCurrentSolution(BoundSonarQubeProject currentBinding);
}

[Export(typeof(IConfigScopeUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class ConfigScopeUpdater : IConfigScopeUpdater
{
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IConnectionIdHelper connectionIdHelper;
    private readonly ISolutionInfoProvider solutionInfoProvider;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public ConfigScopeUpdater(IActiveConfigScopeTracker activeConfigScopeTracker, ISolutionInfoProvider solutionInfoProvider, IConnectionIdHelper connectionIdHelper, IThreadHandling threadHandling)
    {
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.solutionInfoProvider = solutionInfoProvider;
        this.connectionIdHelper = connectionIdHelper;
        this.threadHandling = threadHandling;
    }

    public void UpdateConfigScopeForCurrentSolution(BoundSonarQubeProject currentBinding)
    {
        var solutionName = solutionInfoProvider.GetSolutionName();

        threadHandling.RunOnBackgroundThread(() =>
            {
                HandleConfigScopeUpdateInternal(solutionName,
                    connectionIdHelper.GetConnectionIdFromUri(currentBinding?.ServerUri,
                        currentBinding?.Organization?.Key),
                    currentBinding?.ProjectKey);
                return Task.FromResult(0);
            }).Forget();
    }

    private void HandleConfigScopeUpdateInternal(string solutionName, string connectionId, string projectKey)
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
}
