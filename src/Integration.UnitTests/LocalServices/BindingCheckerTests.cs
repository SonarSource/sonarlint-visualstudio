/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class BindingCheckerTests
    {
        [TestMethod]
        public void IsBindingUpdateRequired_SolutionIsUnbound_True()
        {
            var testSubject = CreateTestSubject(isSolutionBound: false);

            var result = testSubject.IsBindingUpdateRequired();

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsBindingUpdateRequired_SolutionIsUnbound_ProjectBindingIsNotChecked()
        {
            var projectBinder = new Mock<IUnboundProjectFinder>();
            var testSubject = CreateTestSubject(isSolutionBound: false, projectBinder.Object);

            testSubject.IsBindingUpdateRequired();

            projectBinder.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void IsBindingUpdateRequired_SolutionIsBound_NoUnboundProjects_False()
        {
            var unboundProjects = Array.Empty<EnvDTE.Project>();

            var testSubject = CreateTestSubject(unboundProjects);

            var result = testSubject.IsBindingUpdateRequired();

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsBindingUpdateRequired_SolutionIsBound_NoUnboundProjects_NoLogs()
        {
            var unboundProjects = Array.Empty<EnvDTE.Project>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(unboundProjects, logger);

            testSubject.IsBindingUpdateRequired();

            logger.OutputStrings.Should().BeEmpty();
        }

        [TestMethod]
        public void IsBindingUpdateRequired_SolutionIsBound_HasUnboundProjects_True()
        {
            var unboundProjects = new[] { new ProjectMock("unbound.csproj") };

            var testSubject = CreateTestSubject(unboundProjects);

            var result = testSubject.IsBindingUpdateRequired();

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsBindingUpdateRequired_SolutionIsBound_HasUnboundProjects_UnboundProjectsWrittenToLog()
        {
            var unboundProjects = new[] { new ProjectMock("unbound.csproj") };
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(unboundProjects, logger);

            testSubject.IsBindingUpdateRequired();

            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("unbound.csproj");
        }

        private BindingChecker CreateTestSubject(bool isSolutionBound, IUnboundProjectFinder unboundProjectFinder = null, ILogger logger = null)
        {
            var unboundSolutionChecker = new Mock<IUnboundSolutionChecker>();
            unboundSolutionChecker.Setup(x => x.IsBindingUpdateRequired()).Returns(!isSolutionBound);

            logger ??= new TestLogger();

            return new BindingChecker(unboundSolutionChecker.Object, unboundProjectFinder, logger);
        }

        private BindingChecker CreateTestSubject(EnvDTE.Project[] unboundProjects, ILogger logger = null)
        {
            var unboundProjectFinder = new Mock<IUnboundProjectFinder>();
            unboundProjectFinder.Setup(x => x.GetUnboundProjects()).Returns(unboundProjects);

            return CreateTestSubject(true, unboundProjectFinder.Object, logger);
        }
    }
}
