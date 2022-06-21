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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class BindingCheckerTests
    {
        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsUnbound_True()
        {
            var unboundSolutionChecker = CreateUnboundSolutionChecker(isSolutionBound: false);
            var testSubject = CreateTestSubject(unboundSolutionChecker.Object);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeTrue();
            unboundSolutionChecker.VerifyAll();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsUnbound_ProjectBindingIsNotChecked()
        {
            var projectBinder = new Mock<IUnboundProjectFinder>();
            var unboundSolutionChecker = CreateUnboundSolutionChecker(isSolutionBound: false);
            var testSubject = CreateTestSubject(unboundSolutionChecker.Object, projectBinder.Object);

            await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            projectBinder.Invocations.Should().BeEmpty();
            unboundSolutionChecker.VerifyAll();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsBound_NoUnboundProjects_False()
        {
            var unboundProjects = Array.Empty<EnvDTE.Project>();

            var testSubject = CreateTestSubject(unboundProjects);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsBound_NoUnboundProjects_NoLogs()
        {
            var unboundProjects = Array.Empty<EnvDTE.Project>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(unboundProjects, logger);

            await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            logger.OutputStrings.Should().BeEmpty();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsBound_HasUnboundProjects_True()
        {
            var unboundProjects = new[] { new ProjectMock("unbound.csproj") };

            var testSubject = CreateTestSubject(unboundProjects);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsBound_HasUnboundProjects_UnboundProjectsWrittenToLog()
        {
            var unboundProjects = new[] { new ProjectMock("unbound.csproj") };
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(unboundProjects, logger);

            await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("unbound.csproj");
        }

        private Mock<IUnboundSolutionChecker> CreateUnboundSolutionChecker(bool isSolutionBound)
        {
            var unboundSolutionChecker = new Mock<IUnboundSolutionChecker>();
            unboundSolutionChecker.Setup(x => x.IsBindingUpdateRequired(CancellationToken.None)).ReturnsAsync(!isSolutionBound);

            return unboundSolutionChecker;
        }

        private BindingChecker CreateTestSubject(IUnboundSolutionChecker unboundSolutionChecker, IUnboundProjectFinder unboundProjectFinder = null, ILogger logger = null)
        {
            logger ??= new TestLogger();

            return new BindingChecker(unboundSolutionChecker, unboundProjectFinder, logger);
        }

        private BindingChecker CreateTestSubject(EnvDTE.Project[] unboundProjects, ILogger logger = null)
        {
            var unboundProjectFinder = new Mock<IUnboundProjectFinder>();
            unboundProjectFinder.Setup(x => x.GetUnboundProjects()).Returns(unboundProjects);

            var unboundSolutionChecker = CreateUnboundSolutionChecker(isSolutionBound:true);

            return CreateTestSubject(unboundSolutionChecker.Object, unboundProjectFinder.Object, logger);
        }
    }
}
