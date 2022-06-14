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
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Exclusions;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Exclusions
{
    [TestClass]
    public class ExclusionSettingsFileStorageTests
    {
        private const string NotFoundMessage = "Error loading settings for project. File exclusions on the server will not be excluded in the IDE. Error: Settings File was not found";
        private const string objectJson = "{\"Exclusions\":[\"exclusion\"],\"GlobalExclusions\":[\"globalExclusion\"],\"Inclusions\":[\"inclusion1\",\"inclusion2\"]}";
        private const string filePath = "C:\\SolutionPath\\sonar.settings.json";
        private const string fileFolder = "C:\\SolutionPath";

        [TestMethod]
        public void GetSettings_HaveSettings_ReadsSettings()
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(filePath)).Returns(objectJson);
            file.Setup(f => f.Exists(filePath)).Returns(true);

            var logger = new TestLogger();
            
            var testSubject = CreateTestSubject(file.Object, logger);

            var settings = testSubject.GetSettings();

            settings.Should().NotBeNull();
            settings.Inclusions.Count().Should().Be(2);
            settings.Exclusions.Count().Should().Be(1);
            settings.GlobalExclusions.Count().Should().Be(1);

            settings.Inclusions.Contains("inclusion1").Should().BeTrue();
            settings.Inclusions.Contains("inclusion2").Should().BeTrue();
            settings.Exclusions[0].Should().Be("exclusion");
            settings.GlobalExclusions[0].Should().Be("globalExclusion");

            logger.AssertOutputStrings(0);
        }

        [TestMethod]
        public void GetSettings_SettingFileDoesNotExist_ReturnsNull()
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.Exists(filePath)).Returns(false);

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            var settings = testSubject.GetSettings();

            settings.Should().BeNull();
            logger.AssertOutputStrings(1);
            logger.AssertOutputStringExists(NotFoundMessage);
        }

        [TestMethod]
        public void GetSettings_FileReadError_ReturnsNull()
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.Exists(filePath)).Returns(true);
            file.Setup(f => f.ReadAllText(filePath)).Throws(new Exception("File Read Error"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            var settings = testSubject.GetSettings();

            settings.Should().BeNull();
            logger.AssertOutputStrings(1);
            logger.AssertOutputStringDoesNotExist(NotFoundMessage);
            logger.AssertPartialOutputStringExists("File Read Error");
        }

        [TestMethod]
        public void GetSettings_SerializationError_ReturnsNull()
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.Exists(filePath)).Returns(true);
            file.Setup(f => f.ReadAllText(filePath)).Returns("Wrong String");

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            var settings = testSubject.GetSettings();

            settings.Should().BeNull();
            logger.AssertOutputStrings(1);
            logger.AssertOutputStringDoesNotExist(NotFoundMessage);
            logger.AssertPartialOutputStringExists("Unexpected character encountered while parsing value");
        }

        [TestMethod]
        public void GetSettings_StandaloneMode_ReturnsNull()
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(filePath)).Returns(objectJson);
            file.Setup(f => f.Exists(filePath)).Returns(true);

            var bindingConfiguration = BindingConfiguration.Standalone;

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger, bindingConfiguration);

            var settings = testSubject.GetSettings();

            settings.Should().BeNull();
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("File exclusions are not supported in Standalone mode.");
        }

        [TestMethod]
        public void SaveSettings_NoError_SavesSettings()
        {
            var file = new Mock<IFile>();

            var logger = new TestLogger();
            
            var testSubject = CreateTestSubject(file.Object, logger);

            var settings = new ServerExclusions
            {
                Inclusions = new string[] { "inclusion1", "inclusion2" },
                Exclusions = new string[] { "exclusion" },
                GlobalExclusions = new string[] { "globalExclusion" }
            };

            testSubject.SaveSettings(settings, fileFolder);

            file.Verify(f => f.WriteAllText(filePath, objectJson));
            logger.AssertOutputStrings(0);            
        }


        [TestMethod]
        public void SaveSettings_Error_LogsError()
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.WriteAllText(filePath, It.IsAny<string>())).Throws(new Exception());

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            var settings = new ServerExclusions();          

            testSubject.SaveSettings(settings, fileFolder);

            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("Error writing settings for project");
        }

        private ExclusionSettingsFileStorage CreateTestSubject(IFile file, ILogger logger, BindingConfiguration bindingConfiguration = null)
        {
            IFileSystem fileSystem = CreateFileSystem(file);

            bindingConfiguration = bindingConfiguration ?? new BindingConfiguration(null, SonarLintMode.Connected, fileFolder);

            var configurationProviderService = CreateConfigurationProviderService(bindingConfiguration);

            return new ExclusionSettingsFileStorage(logger, fileSystem, configurationProviderService);
        }

        private static IConfigurationProviderService CreateConfigurationProviderService(BindingConfiguration bindingConfiguration)
        {
            var configurationProviderService = new Mock<IConfigurationProviderService>();
            configurationProviderService.Setup(c => c.GetConfiguration()).Returns(bindingConfiguration);
            return configurationProviderService.Object;
        }


        private IFileSystem CreateFileSystem(IFile file)
        {
            var fileSystem = new Mock<IFileSystem>();

            fileSystem.SetupGet(fs => fs.File).Returns(file);

            return fileSystem.Object;
        }
    }
}
