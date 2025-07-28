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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class ResolutionToCountConverterTests
{
    private ResolutionToCountConverter testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new ResolutionToCountConverter();

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Convert_NoRisks_ReturnsZero(bool isResolved)
    {
        var risks = new ObservableCollection<DependencyRiskViewModel>();

        var result = testSubject.Convert([risks, isResolved], null, null, null);

        result.Should().Be("0");
    }

    [TestMethod]
    [DataRow(true, 3)]
    [DataRow(false, 2)]
    public void Convert_MixedRisks_ReturnsCorrectCount(bool isResolved, int expectedCount)
    {
        var risks = new ObservableCollection<DependencyRiskViewModel>
        {
            CreateRiskViewModel(isResolved: true),
            CreateRiskViewModel(isResolved: false),
            CreateRiskViewModel(isResolved: true),
            CreateRiskViewModel(isResolved: false),
            CreateRiskViewModel(isResolved: true)
        };

        var result = testSubject.Convert([risks, isResolved], null, null, null);

        result.Should().Be(expectedCount.ToString());
    }

    [TestMethod]
    public void Convert_RisksNotProvided_ReturnsZero()
    {
        var result = testSubject.Convert([null, true], null, null, null);

        result.Should().Be("0");
    }

    [TestMethod]
    public void Convert_IsResolvedNotProvided_ReturnsZero()
    {
        var risks = new ObservableCollection<DependencyRiskViewModel> { CreateRiskViewModel(isResolved: true), CreateRiskViewModel(isResolved: false) };

        var result = testSubject.Convert([risks, null], null, null, null);

        result.Should().Be("0");
    }

    [TestMethod]
    public void Convert_IsResolvedWrongType_ReturnsZero()
    {
        var risks = new ObservableCollection<DependencyRiskViewModel> { CreateRiskViewModel(isResolved: true), CreateRiskViewModel(isResolved: false) };

        var result = testSubject.Convert([risks, "not a bool"], null, null, null);

        result.Should().Be("0");
    }

    [TestMethod]
    public void Convert_LessParametersThanExpected_ReturnsZero()
    {
        var result = testSubject.Convert([null], null, null, null);

        result.Should().Be("0");
    }

    [TestMethod]
    public void Convert_MoreParametersThanExpected_ConvertsAsExpected()
    {
        var risks = new ObservableCollection<DependencyRiskViewModel> { CreateRiskViewModel(isResolved: true), CreateRiskViewModel(isResolved: false) };

        var result = testSubject.Convert([risks, true, "extra", "parameters"], null, null, null);

        result.Should().Be("1");
    }

    [TestMethod]
    public void ConvertBack_NotImplementedException()
    {
        Action act = () => testSubject.ConvertBack("any", null, null, null);

        act.Should().Throw<NotImplementedException>();
    }

    private static DependencyRiskViewModel CreateRiskViewModel(bool isResolved)
    {
        var dependencyRisk = Substitute.For<IDependencyRisk>();
        dependencyRisk.Transitions.Returns([]);
        var riskStatus = isResolved ? DependencyRiskStatus.Accepted : DependencyRiskStatus.Open;
        dependencyRisk.Status.Returns(riskStatus);
        return new DependencyRiskViewModel(dependencyRisk);
    }
}
