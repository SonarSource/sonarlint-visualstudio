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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class AdditionalFileReferenceCheckerTests
    {
        private static readonly Project ValidProject = new ProjectMock("any.csproj");
        private const string ValidSonarLintFilePath = "c:\\any\\SonarLint.xml";

        private Mock<IProjectSystemHelper> projectSystemHelperMock;
        private AdditionalFileReferenceChecker testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            projectSystemHelperMock = new Mock<IProjectSystemHelper>();

            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemHelperMock.Object);

            testSubject = new AdditionalFileReferenceChecker(serviceProviderMock.Object);
        }

        private void SetFindFileResponse(Project project, string filePath, ProjectItem projectItem) =>
            projectSystemHelperMock.Setup(x => x.FindFileInProject(project, filePath)).Returns(projectItem);

        [TestMethod]
        public void IsReferenced_FileIsNotReferenced_ReturnsFalse()
        {
            SetFindFileResponse(ValidProject, ValidSonarLintFilePath, null);

            var result = testSubject.IsReferenced(ValidProject, ValidSonarLintFilePath);
            result.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null, false)]
        [DataRow("NotAnAdditionalFile", false)]
        [DataRow("AdditionalFile", false)]
        [DataRow("AdditionalFiles", true)]
        [DataRow("ADDITIONALFILES", true)]
        public void IsReferenced_FileIsReferenced_DifferentItemTypes(string itemType, bool expected)
        {
            var projectItem = CreateProjectItemWithItemType(itemType);
            SetFindFileResponse(ValidProject, ValidSonarLintFilePath, projectItem);

            var result = testSubject.IsReferenced(ValidProject, ValidSonarLintFilePath);
            result.Should().Be(expected);
        }

        private static ProjectItem CreateProjectItemWithItemType(string itemType)
        {
            var projectItemMock = new Mock<ProjectItem>();

            var properties = new PropertiesMock(projectItemMock.Object);
            properties.RegisterKnownProperty(Constants.ItemTypePropertyKey).Value = itemType;

            projectItemMock.Setup(x => x.Properties).Returns(properties);
            return projectItemMock.Object;
        }
    }
}
