/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Plugin;
using SonarLint.VisualStudio.SLCore.NodeJS.Notifications;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class PluginListenerTests
{
    private IUnsupportedNodeVersionNotificationService notificationService;
    private IPluginStatusesStore pluginStatusesStore;
    private PluginListener testSubject;

    [TestInitialize]
    public void SetUp()
    {
        notificationService = Substitute.For<IUnsupportedNodeVersionNotificationService>();
        pluginStatusesStore = Substitute.For<IPluginStatusesStore>();
        testSubject = new PluginListener(notificationService, pluginStatusesStore);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<PluginListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IUnsupportedNodeVersionNotificationService>(),
            MefTestHelpers.CreateExport<IPluginStatusesStore>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<PluginListener>();
    }

    [TestMethod]
    [DataRow(Language.TS, "minversion", "currentversion")]
    [DataRow(Language.JS, "min.version", null)]
    public void DidSkipLoadingPlugin_NodeJs_CallsNotificationService(Language language, string minVersion, string currentVersion)
    {
        testSubject.DidSkipLoadingPlugin(new DidSkipLoadingPluginParams(default, language, SkipReason.UNSATISFIED_NODE_JS, minVersion, currentVersion));

        notificationService.Received().Show(language.ToString(), minVersion, currentVersion);
    }

    [TestMethod]
    public void DidSkipLoadingPlugin_Other_Discards()
    {
        testSubject.DidSkipLoadingPlugin(new DidSkipLoadingPluginParams(default, Language.JAVA, SkipReason.UNSATISFIED_JRE, "min", "cur"));

        notificationService.DidNotReceiveWithAnyArgs().Show(default, default, default);
    }

    [TestMethod]
    public void DidChangePluginStatuses_CallsStoreUpdate()
    {
        var configScopeId = "scope1";
        var pluginStatuses = new List<PluginStatusDto>
        {
            new(Language.JAVA, "Java", PluginStateDto.ACTIVE, ArtifactSourceDto.EMBEDDED, "1.0", null, null)
        };

        testSubject.DidChangePluginStatuses(new DidChangePluginStatusesParams(configScopeId, pluginStatuses));

        pluginStatusesStore.Received(1).Update(configScopeId, pluginStatuses);
    }
}
