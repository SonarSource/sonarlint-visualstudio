/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
        public void Mapper_ForProject_KnownLanguage_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => ProjectToLanguageMapper.GetLanguageForProject(null));
        }

        [TestMethod]
        public void Mapper_ForProject_UnknownLanguage_ReturnsUnknown()
        {
            CheckLanguageForProjectKind("", Language.Unknown);
            CheckLanguageForProjectKind("{F63A7EA2-4179-46BF-B9AE-E758BE4EC87C}", Language.Unknown);
            CheckLanguageForProjectKind("wibble", Language.Unknown);
            CheckLanguageForProjectKind("wibble;bibble", Language.Unknown);
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CS_CaseSensitivity1()
        {
            CheckLanguageForProjectKind(ProjectSystemHelper.CSharpProjectKind.ToUpper(), Language.CSharp);
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CS_CaseSensitivity2()
        {
            CheckLanguageForProjectKind(ProjectSystemHelper.CSharpProjectKind.ToLower(), Language.CSharp);
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CS()
        {
            CheckLanguageForProjectKind(ProjectSystemHelper.CSharpProjectKind, Language.CSharp);
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_VB()
        {
            CheckLanguageForProjectKind(ProjectSystemHelper.VbProjectKind, Language.VBNET);
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CSCore()
        {
            CheckLanguageForProjectKind(ProjectSystemHelper.CSharpCoreProjectKind, Language.CSharp);
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_VBCore()
        {
            CheckLanguageForProjectKind(ProjectSystemHelper.VbCoreProjectKind, Language.VBNET);
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_Cpp()
        {
            CheckLanguageForProjectKind(ProjectSystemHelper.CppProjectKind, Language.Cpp);
        }

        private static void CheckLanguageForProjectKind(string projectTypeGuid, Language expectedLanguage)
        {
            // Arrange
            var project = new ProjectMock("any.xxx")
            {
                ProjectKind = projectTypeGuid
            };

            // Act
            var actualLanguage = ProjectToLanguageMapper.GetLanguageForProject(project);

            // Assert
            actualLanguage.Should().Be(expectedLanguage);
        }
    }
}
