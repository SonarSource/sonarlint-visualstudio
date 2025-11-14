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

using SonarLint.VisualStudio.Core.ConfigurationScope;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests;

[TestClass]
public class ServerIssuesConfigurationScopeMonitorTests
{
    private ServerIssuesConfigurationScopeMonitor testSubject;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IServerIssuesSynchronizer serverIssuesSynchronizer;

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        serverIssuesSynchronizer = Substitute.For<IServerIssuesSynchronizer>();
        testSubject = new ServerIssuesConfigurationScopeMonitor(activeConfigScopeTracker, serverIssuesSynchronizer);
    }

    [TestMethod]
    public void Ctor_SubscribesToConfigurationScopeEvents() =>
        activeConfigScopeTracker.Received().CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();

    [TestMethod]
    public void Dispose_UnsubscribesToConfigurationScopeEvents()
    {
        testSubject.Dispose();

        activeConfigScopeTracker.Received().CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
    }

    [TestMethod]
    public void ConfigScopeChangedEvent_CallsServerIssuesSynchronizer()
    {
        var configurationScope = new ConfigurationScope("config scope");
        activeConfigScopeTracker.Current.Returns(configurationScope);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith<ConfigurationScopeChangedEventArgs>(new (default));

        serverIssuesSynchronizer.Received(1).UpdateServerIssuesAsync(configurationScope);
    }
}
