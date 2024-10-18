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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class ActiveSolutionTrackerTests
{
    private ConfigurableServiceProvider serviceProvider;
    private SolutionMock solutionMock;
    private ISolutionInfoProvider solutionInfoProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        this.serviceProvider = new ConfigurableServiceProvider();
        this.solutionMock = new SolutionMock();
        this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        solutionInfoProvider.GetSolutionName().Returns((string)null);
    }

    [TestMethod]
    public void ActiveSolutionTracker_Dispose()
    {
        // Arrange
        int counter = 0;
        var testSubject = CreateTestSubject();
        testSubject.ActiveSolutionChanged += (o, e) => counter++;
        testSubject.Dispose();

        // Act
        this.solutionMock.SimulateSolutionClose();
        this.solutionMock.SimulateSolutionOpen();

        // Assert
        counter.Should().Be(0, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised since disposed");
        solutionInfoProvider.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void ActiveSolutionTracker_RaiseEventOnSolutionOpen()
    {
        // Arrange
        int counter = 0;
        ActiveSolutionChangedEventArgs args = null;
        var testSubject = CreateTestSubject();
        testSubject.ActiveSolutionChanged += (o, e) => { counter++; args = e; };
        solutionInfoProvider.GetSolutionName().Returns("name123");

        // Act
        this.solutionMock.SimulateSolutionOpen();

        // Assert
        counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        args.Should().BeEquivalentTo(new ActiveSolutionChangedEventArgs(true, "name123"));
    }

    [TestMethod]
    public void ActiveSolutionTracker_RaiseEventOnSolutionClose()
    {
        // Arrange
        int counter = 0;
        ActiveSolutionChangedEventArgs args = null;
        var testSubject = CreateTestSubject();
        testSubject.ActiveSolutionChanged += (o, e) => { counter++; args = e; };

        // Act
        this.solutionMock.SimulateSolutionClose();

        // Assert
        counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        args.Should().BeEquivalentTo(new ActiveSolutionChangedEventArgs(false, null));
    }

    [TestMethod]
    public void ActiveSolutionTracker_RaiseEventOnFolderOpen()
    {
        // Arrange
        int counter = 0;
        ActiveSolutionChangedEventArgs args = null;
        var testSubject = CreateTestSubject();
        testSubject.ActiveSolutionChanged += (o, e) => { counter++; args = e; };
        solutionInfoProvider.GetSolutionName().Returns("name123");

        // Act
        solutionMock.SimulateFolderOpen();

        // Assert
        counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
        args.Should().BeEquivalentTo(new ActiveSolutionChangedEventArgs(true, "name123"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void FolderWorkspaceInitializedEvent_RaiseEventOnProjectOpenedIfFolderWorkspace(bool isFolderWorkspace)
    {
        // Arrange
        int counter = 0;
        var testSubject = CreateTestSubject(isFolderWorkspace);
        testSubject.FolderWorkspaceInitialized += (o, e) => { counter++; };

        // Act
        this.solutionMock.SimulateProjectOpen(null);

        // Assert
        counter.Should().Be(isFolderWorkspace ? 1 : 0);
    }

    private ActiveSolutionTracker CreateTestSubject(bool isFolderWorkspace = false)
    {
        var folderWorkspaceFolder = Substitute.For<IFolderWorkspaceService>();
        folderWorkspaceFolder.IsFolderWorkspace().Returns(isFolderWorkspace);
          
        return new ActiveSolutionTracker(serviceProvider, folderWorkspaceFolder, solutionInfoProvider);
    }
}
