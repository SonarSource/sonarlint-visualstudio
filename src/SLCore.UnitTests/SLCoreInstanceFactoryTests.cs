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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.EsLintBridge;
using SonarLint.VisualStudio.SLCore.NodeJS;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.UnitTests;

[TestClass]
public class SLCoreInstanceFactoryTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreInstanceFactory, ISLCoreInstanceFactory>(
            MefTestHelpers.CreateExport<ISLCoreRpcFactory>(),
            MefTestHelpers.CreateExport<ISLCoreRpcManager>(),
            MefTestHelpers.CreateExport<ISLCoreConstantsProvider>(),
            MefTestHelpers.CreateExport<ISLCoreLanguageProvider>(),
            MefTestHelpers.CreateExport<ISLCoreFoldersProvider>(),
            MefTestHelpers.CreateExport<IServerConnectionsProvider>(),
            MefTestHelpers.CreateExport<ISLCoreEmbeddedPluginProvider>(),
            MefTestHelpers.CreateExport<INodeLocationProvider>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<IConfigScopeUpdater>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ISLCoreRuleSettingsProvider>(),
            MefTestHelpers.CreateExport<IEsLintBridgeLocator>(),
            MefTestHelpers.CreateExport<IFocusOnNewCodeService>(),
            MefTestHelpers.CreateExport<ISlCoreTelemetryMigrationProvider>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreInstanceFactory>();
    }

    [TestMethod]
    public void CreateInstance_ReturnsNonNull()
    {
        var islCoreRpcFactory = Substitute.For<ISLCoreRpcFactory>();
        var slCoreRpcManager = Substitute.For<ISLCoreRpcManager>();
        var islCoreConstantsProvider = Substitute.For<ISLCoreConstantsProvider>();
        var slCoreLanguageProvider = Substitute.For<ISLCoreLanguageProvider>();
        var islCoreFoldersProvider = Substitute.For<ISLCoreFoldersProvider>();
        var serverConnectionsProvider = Substitute.For<IServerConnectionsProvider>();
        var slCoreEmbeddedPluginProvider = Substitute.For<ISLCoreEmbeddedPluginProvider>();
        var compatibleNodeLocator = Substitute.For<INodeLocationProvider>();
        var esLintBridgeLocator = Substitute.For<IEsLintBridgeLocator>();
        var activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        var configScopeUpdater = Substitute.For<IConfigScopeUpdater>();
        var threadHandling = Substitute.For<IThreadHandling>();
        var slCoreRuleSettingsProvider = Substitute.For<ISLCoreRuleSettingsProvider>();
        var telemetryMigrationProvider = Substitute.For<ISlCoreTelemetryMigrationProvider>();
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();

        var testSubject = new SLCoreInstanceFactory(
            islCoreRpcFactory,
            slCoreRpcManager,
            islCoreConstantsProvider,
            slCoreLanguageProvider,
            islCoreFoldersProvider,
            serverConnectionsProvider,
            slCoreEmbeddedPluginProvider,
            compatibleNodeLocator,
            activeSolutionBoundTracker,
            configScopeUpdater,
            slCoreRuleSettingsProvider,
            telemetryMigrationProvider,
            esLintBridgeLocator,
            focusOnNewCodeService,
            threadHandling);

        testSubject.CreateInstance().Should().NotBeNull();
    }
}
