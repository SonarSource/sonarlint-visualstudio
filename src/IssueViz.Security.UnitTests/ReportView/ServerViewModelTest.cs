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

using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class ServerViewModelTest
{
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private ServerViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void Ctor_RegistersToEvents() => activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged += Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();

    [TestMethod]
    public void Ctor_InitializesIsCloud()
    {
        activeSolutionBoundTracker.CurrentConfiguration.Returns(CreateBindingConfiguration(new ServerConnection.SonarCloud("myOrg"), SonarLintMode.Connected));
        activeSolutionBoundTracker.ClearReceivedCalls();

        var hotspotsControlViewModel = CreateTestSubject();

        _ = activeSolutionBoundTracker.Received(1).CurrentConfiguration;
        hotspotsControlViewModel.IsCloud.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(SonarLintMode.Connected)]
    [DataRow(SonarLintMode.LegacyConnected)]
    public void SolutionBindingChanged_BindingToCloud_IsCloudIsTrue(SonarLintMode sonarLintMode)
    {
        var cloudBindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarCloud("my org"), sonarLintMode);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(SonarLintMode.Connected)]
    [DataRow(SonarLintMode.LegacyConnected)]
    public void SolutionBindingChanged_BindingToServer_IsCloudIsFalse(SonarLintMode sonarLintMode)
    {
        var cloudBindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarQube(new Uri("C:\\")), sonarLintMode);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeFalse();
    }

    [TestMethod]
    public void SolutionBindingChanged_Standalone_IsCloudIsFalse()
    {
        var cloudBindingConfiguration = new BindingConfiguration(null, SonarLintMode.Standalone, string.Empty);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeFalse();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromActiveSolutionBoundTrackerEvents()
    {
        testSubject.Dispose();

        activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
    }

    private ServerViewModel CreateTestSubject() => new ReportViewModel(activeSolutionBoundTracker, Substitute.For<IDependencyRisksStore>());

    private static BindingConfiguration CreateBindingConfiguration(ServerConnection serverConnection, SonarLintMode mode) =>
        new(new BoundServerProject("my solution", "my project", serverConnection), mode, string.Empty);
}
