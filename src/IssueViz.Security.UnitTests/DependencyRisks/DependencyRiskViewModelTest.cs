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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class DependencyRiskViewModelTest
{
    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        var dependencyRisk = CreateMockedDependencyRisk();

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.DependencyRisk.Should().Be(dependencyRisk);
        testSubject.IsTransitionAllowed.Should().BeFalse();
        testSubject.IssueType.Should().Be(IssueType.DependencyRisk);
    }

    [TestMethod]
    public void Ctor_WithTransitions_InitializesProperties()
    {
        var dependencyRisk = Substitute.For<IDependencyRisk>();
        dependencyRisk.Transitions.Returns([DependencyRiskTransition.Accept]);

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.DependencyRisk.Should().Be(dependencyRisk);
        testSubject.IsTransitionAllowed.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(DependencyRiskImpactSeverity.Info, DisplaySeverity.Info)]
    [DataRow(DependencyRiskImpactSeverity.Low, DisplaySeverity.Low)]
    [DataRow(DependencyRiskImpactSeverity.Medium, DisplaySeverity.Medium)]
    [DataRow(DependencyRiskImpactSeverity.High, DisplaySeverity.High)]
    [DataRow(DependencyRiskImpactSeverity.Blocker, DisplaySeverity.Blocker)]
    public void Ctor_ReturnsCorrectDisplaySeverity(DependencyRiskImpactSeverity dependencyRiskSeverity, DisplaySeverity expectedSeverity)
    {
        var dependencyRisk = CreateMockedDependencyRisk();
        dependencyRisk.Severity.Returns(dependencyRiskSeverity);

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.DisplaySeverity.Should().Be(expectedSeverity);
    }

    [DataTestMethod]
    public void Ctor_UnknownSeverity_ReturnsInfo()
    {
        var dependencyRisk = CreateMockedDependencyRisk();
        dependencyRisk.Severity.Returns((DependencyRiskImpactSeverity)666);

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.DisplaySeverity.Should().Be(DisplaySeverity.Info);
    }

    [DataTestMethod]
    [DataRow(DependencyRiskStatus.Open, false)]
    [DataRow(DependencyRiskStatus.Confirmed, false)]
    [DataRow(DependencyRiskStatus.Accepted, true)]
    [DataRow(DependencyRiskStatus.Safe, true)]
    public void Ctor_SetsIsResolved_Correctly(DependencyRiskStatus status, bool expectedIsResolved)
    {
        var dependencyRisk = CreateMockedDependencyRisk();
        dependencyRisk.Status.Returns(status);

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.IsResolved.Should().Be(expectedIsResolved);
    }

    [TestMethod]
    public void Ctor_SetsIssueViewModelProperties()
    {
        var dependencyRisk = CreateMockedDependencyRisk();
        dependencyRisk.VulnerabilityId.Returns("CVE-2025-123");

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.Title.Should().Be(dependencyRisk.VulnerabilityId);
        testSubject.Line.Should().BeNull();
        testSubject.Column.Should().BeNull();
        testSubject.FilePath.Should().BeNull();
        testSubject.RuleInfo.Should().BeNull();
    }

    [DataTestMethod]
    [DataRow(DependencyRiskStatus.Open, DisplayStatus.Open)]
    [DataRow(DependencyRiskStatus.Confirmed, DisplayStatus.Open)]
    [DataRow(DependencyRiskStatus.Accepted, DisplayStatus.Resolved)]
    [DataRow(DependencyRiskStatus.Fixed, DisplayStatus.Resolved)]
    [DataRow(DependencyRiskStatus.Safe, DisplayStatus.Resolved)]
    public void Ctor_ReturnsCorrectStatus(DependencyRiskStatus hotspotStatus, DisplayStatus expectedStatus)
    {
        var dependencyRisk = CreateMockedDependencyRisk();
        dependencyRisk.Status.Returns(hotspotStatus);

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.Status.Should().Be(expectedStatus);
    }

    [DataTestMethod]
    public void Ctor_UnknownStatus_DefaultsToOpen()
    {
        var dependencyRisk = CreateMockedDependencyRisk();
        dependencyRisk.Status.Returns((DependencyRiskStatus)666);

        var testSubject = new DependencyRiskViewModel(dependencyRisk);

        testSubject.Status.Should().Be(DisplayStatus.Open);
    }

    [TestMethod]
    public void IsOnNewCode_ReturnsTrue()
    {
        var testSubject = new DependencyRiskViewModel(CreateMockedDependencyRisk());

        testSubject.IsOnNewCode.Should().BeTrue();
    }

    private static IDependencyRisk CreateMockedDependencyRisk()
    {
        var dependencyRisk = Substitute.For<IDependencyRisk>();
        dependencyRisk.Transitions.Returns([]);
        return dependencyRisk;
    }
}
