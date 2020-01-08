/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ActiveSolutionTrackerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
        }

        [TestMethod]
        public void ActiveSolutionTracker_Dispose()
        {
            // Arrange
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            testSubject.Dispose();

            // Act
            this.solutionMock.SimulateSolutionClose();
            this.solutionMock.SimulateSolutionOpen();

            // Assert
            counter.Should().Be(0, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised since disposed");
        }

        [TestMethod]
        public void ActiveSolutionTracker_RaiseEventOnSolutionOpen()
        {
            // Arrange
            int counter = 0;
            bool isSolutionOpenArg = false;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => { counter++; isSolutionOpenArg = e.IsSolutionOpen; };

            // Act
            this.solutionMock.SimulateSolutionOpen();

            // Assert
            counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
            isSolutionOpenArg.Should().BeTrue();
        }

        [TestMethod]
        public void ActiveSolutionTracker_RaiseEventOnSolutionClose()
        {
            // Arrange
            int counter = 0;
            bool isSolutionOpenEventArg = true;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => { counter++; isSolutionOpenEventArg = e.IsSolutionOpen; };

            // Act
            this.solutionMock.SimulateSolutionClose();

            // Assert
            counter.Should().Be(1, nameof(testSubject.ActiveSolutionChanged) + " was expected to be raised");
            isSolutionOpenEventArg.Should().BeFalse();
        }

        [TestMethod]
        public void ActiveSolutionTracker_DontRaiseEventOnProjectChanges()
        {
            // Arrange
            int counter = 0;
            var testSubject = new ActiveSolutionTracker(this.serviceProvider);
            testSubject.ActiveSolutionChanged += (o, e) => counter++;
            var project = this.solutionMock.AddOrGetProject("project", isLoaded:false);

            // Act
            this.solutionMock.SimulateProjectLoad(project);
            this.solutionMock.SimulateProjectUnload(project);
            this.solutionMock.SimulateProjectOpen(project);
            this.solutionMock.SimulateProjectClose(project);

            // Assert
            counter.Should().Be(0, nameof(testSubject.ActiveSolutionChanged) + " was not expected to be raised");
        }
    }
}