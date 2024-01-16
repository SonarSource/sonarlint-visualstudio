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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests.CFamilyTestUtility;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject
{
    [TestClass]
    public class FileConfigProviderTests
    {
        [TestMethod]
        public void Get_FileIsNotInSolution_ReturnsNull()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\SingleFileISense\\xxx.vcxproj");
           
            var testSubject = CreateTestSubject();
            var result = testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions());

            result.Should().BeNull();
            projectItemMock.Verify(x => x.ContainingProject, Times.Once);
        }

        [TestMethod]
        public void Get_FailureToCheckIfFileIsInSolution_NonCriticalException_ExceptionCaughtAndNullReturned()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\SingleFileISense\\xxx.vcxproj");
            projectItemMock.Setup(x => x.ContainingProject).Throws<NotImplementedException>();

            var testSubject = CreateTestSubject();
            var result = testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions());

            result.Should().BeNull();
            projectItemMock.Verify(x => x.ContainingProject, Times.Once);
        }

        [TestMethod]
        public void Get_FailureToCheckIfFileIsInSolution_CriticalException_ExceptionThrown()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\SingleFileISense\\xxx.vcxproj");
            projectItemMock.Setup(x => x.ContainingProject).Throws<StackOverflowException>();

            var testSubject = CreateTestSubject();
            Action act = () => testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions());

            act.Should().Throw<StackOverflowException>();
            projectItemMock.Verify(x => x.ContainingProject, Times.Once);
        }

        [TestMethod]
        public void Get_FailsToRetrieveFileConfig_NonCriticalException_ExceptionCaughtAndNullReturned()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj");
            var containingProject = Mock.Get(projectItemMock.Object.ContainingProject.Object as VCProject);
            containingProject.Setup(x => x.ActiveConfiguration).Throws<NotImplementedException>();

            var testSubject = CreateTestSubject();
            var result = testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions());

            result.Should().BeNull();
            containingProject.Verify(x => x.ActiveConfiguration, Times.Once);
        }

        [TestMethod]
        public void Get_FailsToRetrieveFileConfig_CriticalException_ExceptionThrown()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj");
            var containingProject = Mock.Get(projectItemMock.Object.ContainingProject.Object as VCProject);
            containingProject.Setup(x => x.ActiveConfiguration).Throws<StackOverflowException>();

            var testSubject = CreateTestSubject();
            Action act = () => testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions());

            act.Should().Throw<StackOverflowException>();
            containingProject.Verify(x => x.ActiveConfiguration, Times.Once);
        }

        [TestMethod]
        public void Get_FailsToRetrieveFileConfig_NotPch_ExceptionLogged()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj");
            var containingProject = Mock.Get(projectItemMock.Object.ContainingProject.Object as VCProject);
            containingProject.Setup(x => x.ActiveConfiguration).Throws<NotImplementedException>();

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(logger);
            testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions());

            logger.AssertPartialOutputStringExists(nameof(NotImplementedException), "c:\\dummy");
        }

        [TestMethod]
        public void Get_FailsToRetrieveFileConfig_Pch_ExceptionNotLogged()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj");
            var containingProject = Mock.Get(projectItemMock.Object.ContainingProject.Object as VCProject);
            containingProject.Setup(x => x.ActiveConfiguration).Throws<NotImplementedException>();

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(logger);
            testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions{CreatePreCompiledHeaders = true});

            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Get_SuccessfulConfig_ConfigReturned()
        {
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj");
            
            var testSubject = CreateTestSubject();
            var result = testSubject.Get(projectItemMock.Object, "c:\\dummy", new CFamilyAnalyzerOptions());

            result.Should().NotBeNull();
        }

        private FileConfigProvider CreateTestSubject(ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            return new FileConfigProvider(logger);
        }
    }
}
