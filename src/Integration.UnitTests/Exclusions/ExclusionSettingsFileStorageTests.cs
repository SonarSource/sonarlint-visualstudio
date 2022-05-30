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
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Exclusions;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.Exclusions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Exclusions
{
    [TestClass]
    public class ExclusionSettingsFileStorageTests
    {
        private const string NotFoundMessage = "Error loading settings for project projectKey. File exclusions on the server will not be excluded in the IDE. Error: Settings File was not found";
        private const string objectJson = "{\"Inclusions\":[\"inclusion1\",\"inclusion2\"],\"Exclusions\":[\"exclusion\"],\"GlobalInclusions\":[\"globalInclusion\"],\"GlobalExclusions\":[\"globalExclusion\"]}";

        [TestMethod]
        public void GetSettings_HaveSettings_ReadsSettings()
        {
            var projectKey = "projectKey";
            var filePath = GetFilePath(projectKey);

            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(filePath)).Returns(objectJson);
            file.Setup(f => f.Exists(filePath)).Returns(true);
            
            var fileSystem = CreateFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem);

            var settings = testSubject.GetSettings(projectKey);

            settings.Should().NotBeNull();
            settings.Inclusions.Count().Should().Be(2);
            settings.Exclusions.Count().Should().Be(1);
            settings.GlobalInclusions.Count().Should().Be(1);
            settings.GlobalExclusions.Count().Should().Be(1);
        }

        [TestMethod]
        public void GetSettings_HaveNotSettings_ReturnsNull()
        {
            var projectKey = "projectKey";
            var filePath = GetFilePath(projectKey);

            var file = new Mock<IFile>();
            file.Setup(f => f.Exists(filePath)).Returns(false);

            var logger = new Mock<ILogger>();

            var fileSystem = CreateFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem, logger.Object);

            var settings = testSubject.GetSettings(projectKey);

            settings.Should().BeNull();
            logger.Verify(l => l.WriteLine(NotFoundMessage), Times.Once);
        }

        [TestMethod]
        public void GetSettings_HaveError_ReturnsNull()
        {
            var projectKey = "projectKey";
            var filePath = GetFilePath(projectKey);

            var file = new Mock<IFile>();
            file.Setup(f => f.Exists(filePath)).Returns(true);

            var logger = new Mock<ILogger>();

            var fileSystem = CreateFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem, logger.Object);

            var settings = testSubject.GetSettings(projectKey);

            settings.Should().BeNull();
            logger.Verify(l => l.WriteLine(It.IsAny<string>()), Times.Once);
            logger.Verify(l => l.WriteLine(NotFoundMessage), Times.Never);
        }

        [TestMethod]
        public void SaveSettings_NoError_SavesSettings()
        {
            var projectKey = "projectKey";
            var filePath = GetFilePath(projectKey);

            var file = new Mock<IFile>();

            var logger = new Mock<ILogger>();

            var fileSystem = CreateFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem, logger.Object);

            var settings = new ExclusionSettings
            {
                Inclusions = new string[] { "inclusion1", "inclusion2" },
                Exclusions = new string[] { "exclusion" },
                GlobalInclusions = new string[] { "globalInclusion" },
                GlobalExclusions = new string[] { "globalExclusion" }
            };

            testSubject.SaveSettings(projectKey, settings);

            file.Verify(f => f.WriteAllText(filePath, objectJson));
            logger.Verify(l => l.WriteLine(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void SaveSettings_Error_LogsError()
        {
            var projectKey = "projectKey";
            var filePath = GetFilePath(projectKey);

            var file = new Mock<IFile>();
            file.Setup(f => f.WriteAllText(filePath, It.IsAny<string>())).Throws(new Exception());

            var logger = new Mock<ILogger>();

            var fileSystem = CreateFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem, logger.Object);

            var settings = new ExclusionSettings();          

            testSubject.SaveSettings(projectKey, settings);

            logger.Verify(l => l.WriteLine(It.IsAny<string>()), Times.Once);
        }

        private ExclusionSettingsFileStorage CreateTestSubject(IFileSystem fileSystem, ILogger logger = null)
        {
            logger = logger ?? Mock.Of<ILogger>();

            return new ExclusionSettingsFileStorage(logger, fileSystem);
        }

        private IFileSystem CreateFileSystem(IFile file)
        {
            var fileSystem = new Mock<IFileSystem>();
            var directory = Mock.Of<IDirectory>();

            fileSystem.SetupGet(fs => fs.File).Returns(file);
            fileSystem.SetupGet(fs => fs.Directory).Returns(directory);

            return fileSystem.Object;
        }


        private string GetFilePath(string projectKey)
        {
            var exclusionPath = PathHelper.GetTempDirForTask(false, "Exclusions");
            var escapedName = PathHelper.EscapeFileName(projectKey.ToLowerInvariant());
            var fileName = escapedName + ".json";

            var filePath = Path.Combine(exclusionPath, fileName);
            return filePath;
        }
    }
}
