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

using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintIssuesConfigurationScopeMonitorTests
{
    [TestMethod]
    public void Ctor_SubscribesToConfigurationScopeEvents()
    {
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();

        _ = new TaintIssuesConfigurationScopeMonitor(activeConfigScopeTracker, Substitute.For<ITaintIssuesSynchronizer>());

        activeConfigScopeTracker.Received().CurrentConfigurationScopeChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void Dispose_UnsubscribesToConfigurationScopeEvents()
    {
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        var testSubject = new TaintIssuesConfigurationScopeMonitor(activeConfigScopeTracker, Substitute.For<ITaintIssuesSynchronizer>());

        testSubject.Dispose();

        activeConfigScopeTracker.Received().CurrentConfigurationScopeChanged -= Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void ConfigScopeChangedEvent_CallsTaintSynchronizer()
    {
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        var configurationScope = new ConfigurationScope("config scope");
        activeConfigScopeTracker.Current.Returns(configurationScope);
        var taintIssuesSynchronizer = Substitute.For<ITaintIssuesSynchronizer>();
        _ = new TaintIssuesConfigurationScopeMonitor(activeConfigScopeTracker, taintIssuesSynchronizer);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.Event<EventHandler>();

        taintIssuesSynchronizer.Received(1).UpdateTaintVulnerabilitiesAsync(configurationScope);
    }
}
