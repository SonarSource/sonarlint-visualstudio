﻿/*
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

using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintIssuesConfigurationScopeMonitorTests
{
    private TaintIssuesConfigurationScopeMonitor testSubject;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ITaintIssuesSynchronizer taintIssuesSynchronizer;

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        taintIssuesSynchronizer = Substitute.For<ITaintIssuesSynchronizer>();
        testSubject = new TaintIssuesConfigurationScopeMonitor(activeConfigScopeTracker, taintIssuesSynchronizer);
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
    public void ConfigScopeChangedEvent_CallsTaintSynchronizer()
    {
        var configurationScope = new ConfigurationScope("config scope");
        activeConfigScopeTracker.Current.Returns(configurationScope);

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith<ConfigurationScopeChangedEventArgs>(new (default));

        taintIssuesSynchronizer.Received(1).UpdateTaintVulnerabilitiesAsync(configurationScope);
    }
}
