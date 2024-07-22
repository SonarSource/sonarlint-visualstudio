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

using SonarLint.VisualStudio.Integration.NodeJS.Notifications;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Plugin;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class PluginListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<PluginListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IUnsupportedNodeVersionNotificationService>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<PluginListener>();
    }

    [DataTestMethod]
    [DataRow(Language.TS, "minversion", "currentversion")]
    [DataRow(Language.JS, "min.version", null)]
    public void DidSkipLoadingPlugin_NodeJs_CallsNotificationService(Language language, string minVersion, string currentVersion)
    {
        var notificationService = Substitute.For<IUnsupportedNodeVersionNotificationService>();
        var testSubject = CreateTestSubject(notificationService);
        
        testSubject.DidSkipLoadingPlugin(new DidSkipLoadingPluginParams(default, language, SkipReason.UNSATISFIED_NODE_JS, minVersion, currentVersion));
        
        notificationService.Received().Show(language, minVersion, currentVersion);
    }
    
    [TestMethod]
    public void DidSkipLoadingPlugin_Other_Discards()
    {
        var notificationService = Substitute.For<IUnsupportedNodeVersionNotificationService>();
        var testSubject = CreateTestSubject(notificationService);
        
        testSubject.DidSkipLoadingPlugin(new DidSkipLoadingPluginParams(default, Language.JAVA, SkipReason.UNSATISFIED_JRE, "min", "cur"));
        
        notificationService.DidNotReceiveWithAnyArgs().Show(default, default, default);
    }

    private PluginListener CreateTestSubject(IUnsupportedNodeVersionNotificationService notificationService) 
        => new(notificationService);
}
