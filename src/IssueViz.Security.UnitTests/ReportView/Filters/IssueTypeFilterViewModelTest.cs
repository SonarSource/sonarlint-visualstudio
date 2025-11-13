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

using System.ComponentModel;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Filters;

[TestClass]
public class IssueTypeFilterViewModelTest
{
    private IssueTypeFilterViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new IssueTypeFilterViewModel(default);

    [TestMethod]
    [DataRow(IssueType.DependencyRisk)]
    [DataRow(IssueType.SecurityHotspot)]
    [DataRow(IssueType.TaintVulnerability)]
    public void Ctor_InitializesProperties(IssueType issueType)
    {
        var issueTypeFilterViewModel = new IssueTypeFilterViewModel(issueType);

        issueTypeFilterViewModel.IssueType.Should().Be(issueType);
        issueTypeFilterViewModel.IsSelected.Should().BeTrue();
    }

    [TestMethod]
    public void IsSelected_Setter_RaisesPropertyChanged()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.IsSelected = !testSubject.IsSelected;

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(testSubject.IsSelected)));
    }
}
