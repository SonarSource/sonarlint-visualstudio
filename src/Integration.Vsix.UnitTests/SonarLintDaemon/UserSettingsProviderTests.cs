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
using Moq;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class UserSettingsProviderTests
    {

        [TestMethod]
        public void Ctor_NullArguments()
        {
            Action act = () => new UserSettingsProvider(null, new TestLogger(), new FileWrapper());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettingsFilePath");

            act = () => new UserSettingsProvider("", null, new FileWrapper());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new UserSettingsProvider("", new TestLogger(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileWrapper");
        }


        [TestMethod]
        public void Ctor_NoSettingsFile_EmptySettingsReturned()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("nonExistentFile")).Returns(false);

            // Act
            var testSubject = new UserSettingsProvider("nonexistent file", new TestLogger(), fileMock.Object);

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
        }

        [TestMethod]
        public void Ctor_ErrorLoadingSettings_ErrorSquashed_AndEmptySettingsReturned()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(true);
            fileMock.Setup(x => x.ReadAllText("settings.file")).Throws(new System.InvalidOperationException("custom error message"));

            var logger = new TestLogger();

            // Act
            var testSubject = new UserSettingsProvider("settings.file", logger, fileMock.Object);

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
            logger.AssertPartialOutputStringExists("custom error message");
        }

        private static void CheckSettingsAreEmpty(UserSettings userSettings)
        {
            userSettings.Should().NotBeNull();
            userSettings.Rules.Should().NotBeNull();
            userSettings.Rules.Count.Should().Be(0);
        }
    }
}
