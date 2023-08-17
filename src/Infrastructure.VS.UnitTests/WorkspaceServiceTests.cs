/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class WorkspaceServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<WorkspaceService, IWorkspaceService>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void FindRootDirectory_NoIssues_ReturnsDirectory()
        {
            var logger = new Mock<ILogger>();

            var testSubject = CreateTestSubject("Root Dir", logger.Object);

            var result = testSubject.FindRootDirectory();

            result.Should().Be("Root Dir");
            logger.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FindRootDirectory_NoOpenSolution_ReturnsNull()
        {
            var logger = new Mock<ILogger>();
            var testSubject = CreateTestSubject(null, logger.Object, VSConstants.E_UNEXPECTED);

            var result = testSubject.FindRootDirectory();

            result.Should().BeNull();
            logger.Verify(l => l.WriteLine(Resources.NoOpenSolutionOrFolder), Times.Once);
            logger.VerifyNoOtherCalls();
        }

        private WorkspaceService CreateTestSubject(string solution, ILogger logger, int vsConstant = VSConstants.S_OK)
        {
            var vsSolution = SetupVsSolution(solution, vsConstant);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(vsSolution.Object);

            return new WorkspaceService(serviceProvider.Object, logger);
        }

        private static Mock<IVsSolution> SetupVsSolution(object solutionDirectory, int vsConstant)
        {
            var vsSolution = new Mock<IVsSolution>();

            vsSolution
                .Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out solutionDirectory))
                .Returns(vsConstant);

            return vsSolution;
        }
    }
}
