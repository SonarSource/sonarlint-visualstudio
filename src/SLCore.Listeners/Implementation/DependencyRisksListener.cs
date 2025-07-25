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
using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.DependencyRisks;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class DependencyRisksListener(
    IDependencyRisksStore dependencyRisksStore,
    IScaIssueDtoToDependencyRiskConverter converter,
    ILogger logger) : IDependencyRisksListener
{
    private readonly ILogger logger = logger.ForVerboseContext(nameof(DependencyRisksListener));

    [ExcludeFromCodeCoverage]
    public void DidChangeDependencyRisks(DidChangeDependencyRisksParams parameters)
    {
        var currentScope = dependencyRisksStore.CurrentConfigurationScope;
        if (currentScope != parameters.configurationScopeId)
        {
            logger.LogVerbose(SLCoreStrings.ConfigurationScopeMismatch, parameters.configurationScopeId, currentScope);
            return;
        }

        dependencyRisksStore.Update(new DependencyRisksUpdate(
            currentScope,
            parameters.addedDependencyRisks.Select(converter.Convert),
            parameters.updatedDependencyRisks.Select(converter.Convert),
            parameters.closedDependencyRiskIds));
    }
}
