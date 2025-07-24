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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

public interface IShowDependencyRiskInBrowserHandler
{
    void ShowInBrowser(Guid id);
}

[Export(typeof(IShowDependencyRiskInBrowserHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class ShowDependencyRiskInBrowserHandler(
    ISLCoreServiceProvider slCoreServiceProvider,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IThreadHandling threadHandling,
    ILogger logger) : IShowDependencyRiskInBrowserHandler
{
    private readonly ILogger logger = logger.ForContext(Resources.LogContext_DependencyRisks, Resources.LogContext_ShowInBrowser);

    public void ShowInBrowser(Guid id) =>
        threadHandling
            .RunOnBackgroundThread(async () =>
            {
                try
                {
                    if (activeConfigScopeTracker.Current is not { Id: { } configScopeId })
                    {
                        logger.WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
                        return;
                    }
                    if (!slCoreServiceProvider.TryGetTransientService(out IDependencyRiskSlCoreService dependencyRiskSlCoreService))
                    {
                        logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
                        return;
                    }
                    await dependencyRiskSlCoreService.OpenDependencyRiskInBrowserAsync(new(configScopeId, id));
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine(Resources.ShowDependencyRisk_Error_ShowingInBrowser, ex.Message);
                }
            })
            .Forget();
}
