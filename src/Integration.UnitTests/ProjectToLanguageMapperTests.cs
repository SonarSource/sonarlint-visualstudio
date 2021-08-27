﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using SonarLint.VisualStudio.Core.CFamily;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectToLanguageMapperTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ProjectToLanguageMapper, IProjectToLanguageMapper>(null, new[]
            {
                MefTestHelpers.CreateExport<ICMakeProjectTypeIndicator>(Mock.Of<ICMakeProjectTypeIndicator>())
            });
        }

        [TestMethod]
        public void GetAllBindingLanguagesForProject_NullProject_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.GetAllBindingLanguagesForProject(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("{F63A7EA2-4179-46BF-B9AE-E758BE4EC87C}")]
        [DataRow("wibble")]
        [DataRow("wibble;bibble")]
        public void GetAllBindingLanguagesForProject_UnknownProjectType_UnknownProject(string projectTypeGuid)
        {
            CheckGetAllBindingsLanguages(projectTypeGuid, Language.Unknown);
        }

        [TestMethod]
        public void GetAllBindingLanguagesForProject_SupportedProjectType_CaseSensitivity_ReturnsCorrectLanguage()
        {
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CSharpProjectKind.ToUpper(), Language.CSharp);
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CSharpProjectKind.ToLower(), Language.CSharp);
        }

        [TestMethod]
        public void GetAllBindingLanguagesForProject_SupportedProjectType_Cs_ReturnsCorrectLanguage()
        {
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CSharpProjectKind, Language.CSharp);
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CSharpCoreProjectKind, Language.CSharp);
        }

        [TestMethod]
        public void GetAllBindingLanguagesForProject_SupportedProjectType_VB_ReturnsCorrectLanguage()
        {
            CheckGetAllBindingsLanguages(ProjectSystemHelper.VbProjectKind, Language.VBNET);
            CheckGetAllBindingsLanguages(ProjectSystemHelper.VbCoreProjectKind, Language.VBNET);
        }

        [TestMethod]
        public void GetAllBindingLanguagesForProject_SupportedProjectType_CPP_ReturnsCorrectLanguage()
        {
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CppProjectKind, Language.Cpp, Language.C);
        }

        [TestMethod]
        public void GetAllBindingLanguagesForProject_CMakeProject_CFamilyLanguages()
        {
            var cmakeProjectTypeIndicator = SetupCMakeProject(true);

            var project = new ProjectMock("any.xxx") {ProjectKind = Guid.NewGuid().ToString()};

            var testSubject = CreateTestSubject(cmakeProjectTypeIndicator.Object);

            var actualLanguage = testSubject.GetAllBindingLanguagesForProject(project);

            actualLanguage.Should().BeEquivalentTo(Language.Cpp, Language.C);
        }

        [TestMethod]
        public void GetAllBindingLanguagesForProject_NotCMakeProject_UnknownLanguage()
        {
            var cmakeProjectTypeIndicator = SetupCMakeProject(false);

            var project = new ProjectMock("any.xxx") { ProjectKind = Guid.NewGuid().ToString() };

            var testSubject = CreateTestSubject(cmakeProjectTypeIndicator.Object);

            var actualLanguage = testSubject.GetAllBindingLanguagesForProject(project);

            actualLanguage.Should().BeEquivalentTo(Language.Unknown);
        }

        [TestMethod]
        public void HasSupportedLanguage_OnlyUnknownLanguage_False()
        {
            var project = new ProjectMock("any.xxx")
            {
                ProjectKind = "dummy"
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.HasSupportedLanguage(project);

            result.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(ProjectSystemHelper.CppProjectKind)]
        [DataRow(ProjectSystemHelper.CSharpProjectKind)]
        [DataRow(ProjectSystemHelper.CSharpCoreProjectKind)]
        [DataRow(ProjectSystemHelper.VbCoreProjectKind)]
        [DataRow(ProjectSystemHelper.VbProjectKind)]
        public void HasSupportedLanguage_KnownLanguage_True(string projectKind)
        {
            var project = new ProjectMock("any.xxx")
            {
                ProjectKind = projectKind
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.HasSupportedLanguage(project);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void HasSupportedLanguage_NotCMake_False()
        {
            var cmakeProjectTypeIndicator = SetupCMakeProject(false);

            var project = new ProjectMock("any.xxx") { ProjectKind = Guid.NewGuid().ToString() };
            var testSubject = CreateTestSubject(cmakeProjectTypeIndicator.Object);

            var result = testSubject.HasSupportedLanguage(project);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasSupportedLanguageCMake_True()
        {
            var cmakeProjectTypeIndicator = SetupCMakeProject(true);
            
            var project = new ProjectMock("any.xxx") { ProjectKind = Guid.NewGuid().ToString() };
            var testSubject = CreateTestSubject(cmakeProjectTypeIndicator.Object);

            var result = testSubject.HasSupportedLanguage(project);

            result.Should().BeTrue();
        }

        private static void CheckGetAllBindingsLanguages(string projectTypeGuid, params Language[] expectedLanguages)
        {
            // Arrange
            var project = new ProjectMock("any.xxx")
            {
                ProjectKind = projectTypeGuid
            };

            var testSubject = CreateTestSubject();

            // Act
            var actualLanguage = testSubject.GetAllBindingLanguagesForProject(project);

            // Assert
            actualLanguage.Should().BeEquivalentTo(expectedLanguages);
        }

        private static IProjectToLanguageMapper CreateTestSubject(ICMakeProjectTypeIndicator cmakeProjectTypeIndicator = null)
        {
            cmakeProjectTypeIndicator ??= Mock.Of<ICMakeProjectTypeIndicator>();

            return new ProjectToLanguageMapper(cmakeProjectTypeIndicator);
        }

        private static Mock<ICMakeProjectTypeIndicator> SetupCMakeProject(bool isCMake)
        {
            var cmakeProjectTypeIndicator = new Mock<ICMakeProjectTypeIndicator>();

            cmakeProjectTypeIndicator
                .Setup(x => x.IsCMake())
                .Returns(isCMake);

            return cmakeProjectTypeIndicator;
        }
    }
}
