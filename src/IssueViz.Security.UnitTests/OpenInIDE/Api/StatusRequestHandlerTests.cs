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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Api
{
    [TestClass]
    public class StatusRequestHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<StatusRequestHandler, IStatusRequestHandler>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>())
            });
        }

        [TestMethod]
        public async Task GetStatusAsync_FailedToFetchVsName_ReturnsDefaultIdeName()
        {
            var solution = SetupSolutionName("");
            var shell = SetupIdeName(null);

            var testSubject = CreateTestSubject(solution, shell);
            var result = await testSubject.GetStatusAsync();

            result.IdeName.Should().Be("Microsoft Visual Studio");
        }

        [TestMethod]
        public async Task GetStatusAsync_FailedToFetchSolutionName_ReturnsEmptySolutionName()
        {
            var solution = SetupSolutionName(null);
            var shell = SetupIdeName("some ide name");

            var testSubject = CreateTestSubject(solution, shell);
            var result = await testSubject.GetStatusAsync();

            result.Description.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetStatusAsync_VsNameFetched_ReturnsFetchedIdeName()
        {
            var solution = SetupSolutionName("some solution");
            var shell = SetupIdeName("some ide name");

            var testSubject = CreateTestSubject(solution, shell);
            var result = await testSubject.GetStatusAsync();

            result.IdeName.Should().Be("some ide name");
        }

        [TestMethod]
        public async Task GetStatusAsync_SolutionNameFetched_ReturnsFetchedName()
        {
            var solution = SetupSolutionName("some solution");
            var shell = SetupIdeName(null);

            var testSubject = CreateTestSubject(solution, shell);
            var result = await testSubject.GetStatusAsync();

            result.Description.Should().Be("some solution");
        }

        private static IStatusRequestHandler CreateTestSubject(Mock<IVsSolution> solution, Mock<IVsShell> shell)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(solution.Object);
            serviceProvider.Setup(x => x.GetService(typeof(SVsShell))).Returns(shell.Object);

            return new StatusRequestHandler(serviceProvider.Object);
        }

        private static Mock<IVsSolution> SetupSolutionName(object name)
        {
            var solution = new Mock<IVsSolution>();
            solution.Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionBaseName, out name));
            return solution;
        }

        private static Mock<IVsShell> SetupIdeName(object name)
        {
            var shell = new Mock<IVsShell>();
            shell.Setup(x => x.GetProperty((int) __VSSPROPID5.VSSPROPID_AppBrandName, out name));

            return shell;
        }
    }
}
