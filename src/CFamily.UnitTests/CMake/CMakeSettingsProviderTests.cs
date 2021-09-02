/*
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
using System.IO;
using System.IO.Abstractions;
using CMakeSettingsProviderTestsExtensions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Integration.UnitTests.Extensions;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class CMakeSettingsProviderTests
    {
        private const string RootDirectory = "dummy root";

        [TestMethod]
        public void TryGet_FileDoesNotExist_Null()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetFileDoesNotExist(cmakeSettingsLocation);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.TryGet(RootDirectory);

            result.Should().BeNull();
        }

        [TestMethod]
        public void TryGet_FileExists_ParsedSettingsReturned()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);
            var cmakeSettings = new CMakeSettings
            {
                Configurations = new[]
                {
                    new CMakeBuildConfiguration { BuildRoot = "root1", Name = "name1" },
                    new CMakeBuildConfiguration { BuildRoot = "root2", Name = "name2" }
                }
            };

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupCMakeSettingsFileExists(cmakeSettingsLocation, cmakeSettings);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.TryGet(RootDirectory);

            result.Should().BeEquivalentTo(cmakeSettings);
        }

        [TestMethod]
        public void TryGet_FailedToReadCMakeSettings_NonCriticalException_Null()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetFileExists(cmakeSettingsLocation);
            fileSystem
                .Setup(x => x.File.ReadAllText(cmakeSettingsLocation))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(fileSystem.Object, logger);

            var result = testSubject.TryGet(RootDirectory);

            result.Should().BeNull();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void TryGet_FailedToReadCMakeSettings_CriticalException_ExceptionThrown()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetFileExists(cmakeSettingsLocation);
            fileSystem
                .Setup(x => x.File.ReadAllText(cmakeSettingsLocation))
                .Throws(new StackOverflowException());

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(fileSystem.Object, logger);

            Action act = () => testSubject.TryGet(RootDirectory);

            act.Should().Throw<StackOverflowException>();
        }

        [TestMethod]
        public void TryGet_FailedToParseCMakeSettings_Null()
        {
            const string invalidJson = "invalid json";
            var expectedMessage = GetExpectedDeserializationMessage();
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetFileReadAllText(cmakeSettingsLocation, invalidJson);

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(fileSystem.Object, logger);

            var result = testSubject.TryGet(RootDirectory);

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

        private static string GetCmakeSettingsLocation(string rootDirectory) =>
            Path.GetFullPath(Path.Combine(rootDirectory, CMakeSettingsProvider.CMakeSettingsFileName));

        private CMakeSettingsProvider CreateTestSubject(IFileSystem fileSystem, ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();
            return new CMakeSettingsProvider(logger, fileSystem);
        }
    }
}

namespace CMakeSettingsProviderTestsExtensions
{
    // Extension methods specific to this set of tests.
    // They are in an inner namespace to stop them appearing for other tests
    internal static class Extensions
    {
        public static Mock<IFileSystem> SetupCMakeSettingsFileExists(this Mock<IFileSystem> fileSystem,
            string cmakeSettingsLocation, CMakeSettings cmakeSettings) =>
            fileSystem.SetFileReadAllText(cmakeSettingsLocation, JsonConvert.SerializeObject(cmakeSettings));
    }
}
