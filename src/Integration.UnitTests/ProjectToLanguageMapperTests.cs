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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectToLanguageMapperTests
    {
        [TestMethod]
        public void Mapper_GetLanguage_KnownLanguage_ArgChecks()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Exceptions.Expect<ArgumentNullException>(() => ProjectToLanguageMapper.GetLanguageForProject(null));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [TestMethod]
        public void Mapper_GetLanguage_UnknownLanguage_ReturnsUnknown()
        {
            CheckGetLanguage("", Language.Unknown);
            CheckGetLanguage("{F63A7EA2-4179-46BF-B9AE-E758BE4EC87C}", Language.Unknown);
            CheckGetLanguage("wibble", Language.Unknown);
            CheckGetLanguage("wibble;bibble", Language.Unknown);
        }

        [TestMethod]
        public void Mapper_GetLanguage_KnownLanguage_ReturnsCorrectLanguage_CS_CaseSensitivity()
        {
            CheckGetLanguage(ProjectSystemHelper.CSharpProjectKind.ToUpper(), Language.CSharp);
            CheckGetLanguage(ProjectSystemHelper.CSharpProjectKind.ToLower(), Language.CSharp);
        }

        [TestMethod]
        public void Mapper_GetLanguage_KnownLanguage_ReturnsCorrectLanguage_CS()
        {
            CheckGetLanguage(ProjectSystemHelper.CSharpProjectKind, Language.CSharp);
            CheckGetLanguage(ProjectSystemHelper.CSharpCoreProjectKind, Language.CSharp);
        }

        [TestMethod]
        public void Mapper_GetLanguage_KnownLanguage_ReturnsCorrectLanguage_VB()
        {
            CheckGetLanguage(ProjectSystemHelper.VbProjectKind, Language.VBNET);
            CheckGetLanguage(ProjectSystemHelper.VbCoreProjectKind, Language.VBNET);
        }

        [TestMethod]
        public void Mapper_GetLanguage_KnownLanguage_ReturnsCorrectLanguage_Cpp()
        {
            CheckGetLanguage(ProjectSystemHelper.CppProjectKind, Language.Cpp);
        }

        [TestMethod]
        public void Mapper_AllBindingLanguages_CSharpProject()
        {
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CSharpProjectKind, Language.CSharp);
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CSharpCoreProjectKind, Language.CSharp);
        }

        [TestMethod]
        public void Mapper_AllBindingLanguages_VbProject()
        {
            CheckGetAllBindingsLanguages(ProjectSystemHelper.VbProjectKind, Language.VBNET);
            CheckGetAllBindingsLanguages(ProjectSystemHelper.VbCoreProjectKind, Language.VBNET);
        }

        [TestMethod]
        public void Mapper_AllBindingLanguages_CppProject()
        {
            CheckGetAllBindingsLanguages(ProjectSystemHelper.CppProjectKind, Language.Cpp, Language.C);
        }

        [TestMethod]
        public void Mapper_AllBindingLanguages_UnknownProject()
        {
            CheckGetAllBindingsLanguages(Guid.NewGuid().ToString(), Language.Unknown);
        }

        private static void CheckGetLanguage(string projectTypeGuid, Language expectedLanguage)
        {
            // Arrange
            var project = new ProjectMock("any.xxx")
            {
                ProjectKind = projectTypeGuid
            };

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            var actualLanguage = ProjectToLanguageMapper.GetLanguageForProject(project);
#pragma warning restore CS0618 // Type or member is obsolete

            // Assert
            actualLanguage.Should().Be(expectedLanguage);
        }

        private static void CheckGetAllBindingsLanguages(string projectTypeGuid, params Language[] expectedLanguages)
        {
            // Arrange
            var project = new ProjectMock("any.xxx")
            {
                ProjectKind = projectTypeGuid
            };

            // Act
            var actualLanguage = ProjectToLanguageMapper.GetAllBindingLanguagesForProject(project);

            // Assert
            actualLanguage.Should().BeEquivalentTo(expectedLanguages);
        }

    }
}
