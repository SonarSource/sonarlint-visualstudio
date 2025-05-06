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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings.SolutionSettings;

[TestClass]
public class SolutionSettingsViewModelTest
{
    private const string SolutionName = "mySolution";
    private SolutionSettingsViewModel testSubject;
    private ISolutionInfoProvider solutionInfoProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        testSubject = new SolutionSettingsViewModel(solutionInfoProvider);
    }

    [TestMethod]
    public void ConnectionInfo_Set_RaisesPropertyChanged()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.Description = "test";

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.Description)));
    }

    [TestMethod]
    public void InitializeDescription_SolutionOpened_IsExpected()
    {
        solutionInfoProvider.GetSolutionName().Returns(SolutionName);
        solutionInfoProvider.IsFolderWorkspace().Returns(false);

        testSubject.InitializeDescription();

        testSubject.Description.Should().Be(string.Format(Strings.SolutionSettingsDialog_Description, "solution", SolutionName));
    }

    [TestMethod]
    public void InitializeDescription_WorkspaceOpened_IsExpected()
    {
        solutionInfoProvider.GetSolutionName().Returns(SolutionName);
        solutionInfoProvider.IsFolderWorkspace().Returns(true);

        testSubject.InitializeDescription();

        testSubject.Description.Should().Be(string.Format(Strings.SolutionSettingsDialog_Description, "folder", SolutionName));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void InitializeDescription_NoSolution_IsExpected(bool isWorkspace)
    {
        solutionInfoProvider.GetSolutionName().Returns((string)null);
        solutionInfoProvider.IsFolderWorkspace().Returns(isWorkspace);

        testSubject.InitializeDescription();

        testSubject.Description.Should().BeNull();
    }
}
