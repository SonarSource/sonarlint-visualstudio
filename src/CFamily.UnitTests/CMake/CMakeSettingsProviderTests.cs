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
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class CMakeSettingsProviderTests
    {
        private const string RootDirectory = "c:\\dummy root";

        [TestMethod]
        public void Find_FileDoesNotExist_Null()
        {
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory(RootDirectory);

            var testSubject = CreateTestSubject(fileSystem);

            var result = testSubject.Find(RootDirectory);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Find_FileExists_ParsedSettingsReturned()
        {
            var cmakeSettings = new CMakeSettings
            {
                Configurations = new[]
                {
                    new CMakeBuildConfiguration { BuildRoot = "root1", Name = "name1", Generator = "gen1"},
                    new CMakeBuildConfiguration { BuildRoot = "root2", Name = "name2", Generator = "gen2"}
                }
            };

            var cmakeSettingsFile = Path.Combine(RootDirectory, CMakeSettingsProvider.CMakeSettingsFileName);
            var cmakeListsFile = Path.Combine(RootDirectory, CMakeSettingsProvider.CMakeListsFileName);

            var fileSystem = SetupFileSystem(
                (cmakeSettingsFile, JsonConvert.SerializeObject(cmakeSettings)),
                (cmakeListsFile, "")
            );

            var testSubject = CreateTestSubject(fileSystem);

            var result = testSubject.Find(RootDirectory);

            result.Should().NotBeNull();
            result.Settings.Should().BeEquivalentTo(cmakeSettings);
            result.CMakeSettingsFilePath.Should().Be(cmakeSettingsFile);
            result.RootCMakeListsFilePath.Should().Be(cmakeListsFile);
        }

        [TestMethod]
        public void Find_FileExists_NoCMakeListsNextToIt_Null()
        {
            var cmakeSettings = new CMakeSettings
            {
                Configurations = new[]
                {
                    new CMakeBuildConfiguration { BuildRoot = "root1", Name = "name1" },
                    new CMakeBuildConfiguration { BuildRoot = "root2", Name = "name2" }
                }
            };

            var cmakeSettingsFile = Path.Combine(RootDirectory, CMakeSettingsProvider.CMakeSettingsFileName);

            var fileSystem = SetupFileSystem(
                (cmakeSettingsFile, JsonConvert.SerializeObject(cmakeSettings))
            );

            var testSubject = CreateTestSubject(fileSystem);

            var result = testSubject.Find(RootDirectory);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Find_FailedToParseCMakeSettings_Null()
        {
            const string invalidJson = "invalid json";
            var expectedMessage = GetExpectedDeserializationMessage();
            var cmakeSettingsFile = Path.Combine(RootDirectory, CMakeSettingsProvider.CMakeSettingsFileName);
            var cmakeListsFile = Path.Combine(RootDirectory, CMakeSettingsProvider.CMakeListsFileName);

            var fileSystem = SetupFileSystem(
                (cmakeSettingsFile, invalidJson),
                (cmakeListsFile, "")
            );

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(fileSystem, logger);

            var result = testSubject.Find(RootDirectory);

            result.Should().BeNull();

            logger.AssertPartialOutputStringExists(expectedMessage);

            string GetExpectedDeserializationMessage()
            {
                try
                {
                    JsonConvert.DeserializeObject(invalidJson);
                }
                catch (JsonReaderException ex)
                {
                    return ex.Message;
                }

                return null;
            }
        }

        [TestMethod]
        [DataRow("c:\\root\\CMakeLists.txt")]
        [DataRow("c:\\root\\sub\\CMakeLists.txt")]
        [DataRow("c:\\root\\sub\\other\\CMakeLists.txt")]
        public void Find_MultipleSettingsFiles_ReturnsFileThatHasCMakeListsNextToIt(string locationOfRootCMakeLists)
        {
            const string rootDirectory = "c:\\root";

            var fileSystem = SetupFileSystem(
                ("c:\\root\\CMakeSettings.json", JsonConvert.SerializeObject(new CMakeSettings())),
                ("c:\\root\\sub\\CMakeSettings.json", JsonConvert.SerializeObject(new CMakeSettings())),
                ("c:\\root\\sub\\other\\CMakeSettings.json", JsonConvert.SerializeObject(new CMakeSettings())),
                ("c:\\otherRoot\\CMakeSettings.json", JsonConvert.SerializeObject(new CMakeSettings())),
                (locationOfRootCMakeLists, "")
            );

            var testSubject = CreateTestSubject(fileSystem);

            var result = testSubject.Find(rootDirectory);

            result.Should().NotBeNull();
            result.RootCMakeListsFilePath.Should().Be(locationOfRootCMakeLists);

            var expectedCMakeSettingsFileLocation = Path.Combine(
                Path.GetDirectoryName(locationOfRootCMakeLists),
                "CMakeSettings.json");

            result.CMakeSettingsFilePath.Should().Be(expectedCMakeSettingsFileLocation);
        }

        [TestMethod]
        public void Find_MultipleMatches_ReturnsTopMost()
        {
            const string rootDirectory = "c:\\root";

            var fileSystem = SetupFileSystem(
                ("c:\\root\\src\\CMakeSettings.json", JsonConvert.SerializeObject(new CMakeSettings())),
                ("c:\\root\\src\\CMakeLists.txt", ""),
                ("c:\\root\\src\\sub\\CMakeSettings.json", JsonConvert.SerializeObject(new CMakeSettings())),
                ("c:\\root\\src\\sub\\CMakeLists.json", "")
            );

            var testSubject = CreateTestSubject(fileSystem);

            var result = testSubject.Find(rootDirectory);

            result.Should().NotBeNull();
            result.RootCMakeListsFilePath.Should().Be("c:\\root\\src\\CMakeLists.txt");
            result.CMakeSettingsFilePath.Should().Be("c:\\root\\src\\CMakeSettings.json");
        }

        [TestMethod]
        public void Find_FailedToFindCMakeSettings_NonCriticalException_Null()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem
                .Setup(x => x.Directory)
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(fileSystem.Object, logger);

            var result = testSubject.Find(RootDirectory);

            result.Should().BeNull();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void Find_FailedToFindCMakeSettings_CriticalException_ExceptionThrown()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem
                .Setup(x => x.Directory)
                .Throws(new StackOverflowException());

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(fileSystem.Object, logger);

            Action act = () => testSubject.Find(RootDirectory);

            act.Should().Throw<StackOverflowException>();
        }

        private CMakeSettingsProvider CreateTestSubject(IFileSystem fileSystem, ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();
            return new CMakeSettingsProvider(logger, fileSystem);
        }

        private IFileSystem SetupFileSystem(params (string, string)[] pathsAndContents)
        {
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory(RootDirectory);

            foreach (var filePathAndContent in pathsAndContents)
            {
                fileSystem.AddFile(filePathAndContent.Item1, new MockFileData(filePathAndContent.Item2));
            }

            return fileSystem;
        }
    }
}
