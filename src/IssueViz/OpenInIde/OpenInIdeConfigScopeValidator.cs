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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

internal interface IOpenInIdeConfigScopeValidator
{
    bool TryGetConfigurationScopeRoot(string issueConfigurationScope, out string configurationScopeRoot);
}

[Export(typeof(IOpenInIdeConfigScopeValidator))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenInIdeConfigScopeValidator : IOpenInIdeConfigScopeValidator
{
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly ILogger logger;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public OpenInIdeConfigScopeValidator(IActiveConfigScopeTracker activeConfigScopeTracker, ILogger logger, IThreadHandling threadHandling)
    {
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.logger = logger;
        this.threadHandling = threadHandling;
    }

    public bool TryGetConfigurationScopeRoot(string issueConfigurationScope, out string configurationScopeRoot)
    {
        configurationScopeRoot = default;
        threadHandling.ThrowIfOnUIThread();

        var configScope = activeConfigScopeTracker.Current;

        if (configScope is null || configScope.Id != issueConfigurationScope)
        {
            logger.WriteLine(OpenInIdeResources.ApiHandler_ConfigurationScopeMismatch, configScope, issueConfigurationScope);
            return false;
        }

        if (configScope.SonarProjectId == null)
        {
            logger.WriteLine(OpenInIdeResources.ApiHandler_ConfigurationScopeNotBound);
            return false;
        }

        if (configScope.RootPath == null)
        {
            logger.WriteLine(OpenInIdeResources.ApiHandler_ConfigurationScopeRootNotSet);
            return false;
        }

        configurationScopeRoot = configScope.RootPath;
        return true;
    }
}
