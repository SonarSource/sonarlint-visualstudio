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
            // Arrange
            var otherProject = new ProjectMock("other.proj");

            // Act
            var otherProjectLanguage = ProjectToLanguageMapper.GetLanguageForProject(otherProject);

            // Assert
            otherProjectLanguage.Should().Be(Language.Unknown, "Unexpected Language for unknown project");
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CS_CaseSensitivity1()
        {
            // Arrange
            var csProject = new ProjectMock("cs1.csproj")
            {
                ProjectKind = ProjectSystemHelper.CSharpProjectKind.ToUpper()
            };

            // Act
            var csProjectLanguage = ProjectToLanguageMapper.GetLanguageForProject(csProject);

            // Assert
            csProjectLanguage.Should().Be(Language.CSharp, "Unexpected Language for C# project");
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CS_CaseSensitivity2()
        {
            // Arrange
            var csProject = new ProjectMock("cs1.csproj")
            {
                ProjectKind = ProjectSystemHelper.CSharpProjectKind.ToLower()
            };

            // Act
            var csProjectLanguage = ProjectToLanguageMapper.GetLanguageForProject(csProject);

            // Assert
            csProjectLanguage.Should().Be(Language.CSharp, "Unexpected Language for C# project");
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CS()
        {
            // Arrange
            var csProject = new ProjectMock("cs1.csproj");
            csProject.SetCSProjectKind();

            // Act
            var csProjectLanguage = ProjectToLanguageMapper.GetLanguageForProject(csProject);

            // Assert
            csProjectLanguage.Should().Be(Language.CSharp, "Unexpected Language for C# project");
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_VB()
        {
            // Test case 3: VB - non-Core
            // Arrange
            var vbNetProject = new ProjectMock("vb1.vbproj");
            vbNetProject.SetVBProjectKind();

            // Act
            var vbNetProjectLanguage = ProjectToLanguageMapper.GetLanguageForProject(vbNetProject);

            // Assert
            vbNetProjectLanguage.Should().Be(Language.VBNET, "Unexpected Language for VB project");
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_CSCore()
        {
            // Arrange
            var csProject = new ProjectMock("cs1.csproj")
            {
                ProjectKind = ProjectSystemHelper.CSharpCoreProjectKind
            };

            // Act
            var csProjectLanguage = ProjectToLanguageMapper.GetLanguageForProject(csProject);

            // Assert
            csProjectLanguage.Should().Be(Language.CSharp, "Unexpected Language for C# Core project");
        }

        [TestMethod]
        public void Mapper_ForProject_KnownLanguage_ReturnsCorrectLanguage_VBCore()
        {
            // Arrange
            var vbNetProject = new ProjectMock("vb1.vbproj")
            {
                ProjectKind = ProjectSystemHelper.VbCoreProjectKind
            };

            // Act
            var vbNetProjectLanguage = ProjectToLanguageMapper.GetLanguageForProject(vbNetProject);

            // Assert
            vbNetProjectLanguage.Should().Be(Language.VBNET, "Unexpected Language for VB Core project");
        }
    }
}
