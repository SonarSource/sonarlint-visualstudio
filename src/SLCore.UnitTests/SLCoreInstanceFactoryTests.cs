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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Configuration;
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
            MefTestHelpers.CreateExport<ISLCoreConstantsProvider>(),
            MefTestHelpers.CreateExport<ISLCoreFoldersProvider>(),
            MefTestHelpers.CreateExport<IServerConnectionsProvider>(),
            MefTestHelpers.CreateExport<ISLCoreEmbeddedPluginJarLocator>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
            MefTestHelpers.CreateExport<IConfigScopeUpdater>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
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
        var islCoreConstantsProvider = Substitute.For<ISLCoreConstantsProvider>();
        var islCoreFoldersProvider = Substitute.For<ISLCoreFoldersProvider>();
        var serverConnectionsProvider = Substitute.For<IServerConnectionsProvider>();
        var islCoreEmbeddedPluginJarLocator = Substitute.For<ISLCoreEmbeddedPluginJarLocator>();
        var activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        var configScopeUpdater = Substitute.For<IConfigScopeUpdater>();
        var threadHandling = Substitute.For<IThreadHandling>();
        var testSubject = new SLCoreInstanceFactory(islCoreRpcFactory, islCoreConstantsProvider, islCoreFoldersProvider, serverConnectionsProvider, islCoreEmbeddedPluginJarLocator, activeSolutionBoundTracker, configScopeUpdater, threadHandling);

        testSubject.CreateInstance().Should().NotBeNull();
    }
}
