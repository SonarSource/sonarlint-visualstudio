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

using System.ComponentModel;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class GroupDependencyRiskViewModelTest
{
    private GroupDependencyRiskViewModel testSubject;

    [TestInitialize]
    public void Initialize() => testSubject = new GroupDependencyRiskViewModel();

    [TestMethod]
    public void Ctor_HasPropertiesInitialized()
    {
        GroupDependencyRiskViewModel.Title.Should().Be(Resources.DependencyRisksGroupTitle);
        testSubject.Risks.Should().BeEmpty();
    }

    [TestMethod]
    public void InitializeRisks_InitializesRisks()
    {
        testSubject.InitializeRisks();

        testSubject.Risks.Should().HaveCount(3);
    }

    [TestMethod]
    public void InitializeRisks_RaisesPropertyChanged()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.InitializeRisks();

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasRisks)));
    }

    [TestMethod]
    public void HasRisks_ReturnsTrue_WhenThereAreRisks()
    {
        testSubject.InitializeRisks();

        testSubject.HasRisks.Should().BeTrue();
    }

    [TestMethod]
    public void HasRisks_ReturnsFalse_WhenThereAreRisks() => testSubject.HasRisks.Should().BeFalse();
}
