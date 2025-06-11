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
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily;

[Export(typeof(IActiveCompilationDatabaseTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class ActiveCompilationDatabaseTracker : IActiveCompilationDatabaseTracker
{
    private readonly ICMakeCompilationDatabaseLocator cMakeCompilationDatabaseLocator;
    private readonly IActiveVcxCompilationDatabase activeVcxCompilationDatabase;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly IAsyncLock asyncLock;
    private bool isDisposed;
    private string lastConfigScopeId;

    public IInitializationProcessor InitializationProcessor { get; }
    public string DatabasePath { get; private set; }

    [ImportingConstructor]
    public ActiveCompilationDatabaseTracker(
        ICMakeCompilationDatabaseLocator cMakeCompilationDatabaseLocator,
        IActiveVcxCompilationDatabase activeVcxCompilationDatabase,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IInitializationProcessorFactory initializationProcessorFactory,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling,
        ISLCoreServiceProvider serviceProvider)
    {
        this.cMakeCompilationDatabaseLocator = cMakeCompilationDatabaseLocator;
        this.activeVcxCompilationDatabase = activeVcxCompilationDatabase;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.threadHandling = threadHandling;
        this.serviceProvider = serviceProvider;
        asyncLock = asyncLockFactory.Create();
        InitializationProcessor = initializationProcessorFactory.CreateAndStart<ActiveCompilationDatabaseTracker>([],
            async () =>
            {
                if (isDisposed)
                {
                    return;
                }
                await activeVcxCompilationDatabase.EnsureDatabaseInitializedAsync();
                activeConfigScopeTracker.CurrentConfigurationScopeChanged += ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
                await HandleConfigScopeEventAsync();
            });
    }

    private void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged(object sender, ConfigurationScopeChangedEventArgs e)
    {
        if (!e.DefinitionChanged)
        {
            return;
        }
        threadHandling.RunOnBackgroundThread(HandleConfigScopeEventAsync).Forget();
    }

    private async Task HandleConfigScopeEventAsync()
    {
        using (await asyncLock.AcquireAsync())
        {
            if (activeConfigScopeTracker.Current is { Id: { } currentConfigScopeId } && serviceProvider.TryGetTransientService(out ICFamilyAnalysisConfigurationSLCoreService cFamilyAnalysisConfiguration))
            {
                lastConfigScopeId = currentConfigScopeId;
                DatabasePath = cMakeCompilationDatabaseLocator.Locate() ?? activeVcxCompilationDatabase.DatabasePath;
                cFamilyAnalysisConfiguration.DidChangePathToCompileCommands(new(currentConfigScopeId, DatabasePath));
            }
            else
            {
                lastConfigScopeId = null;
                DatabasePath = null;
            }
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        if (InitializationProcessor.IsFinalized)
        {
            activeConfigScopeTracker.CurrentConfigurationScopeChanged -= ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
            threadHandling.Run(async () =>
            {
                await activeVcxCompilationDatabase.DropDatabaseAsync();
                return 0;
            });
        }

        activeVcxCompilationDatabase.Dispose();
        asyncLock.Dispose();
        isDisposed = true;
    }
}
