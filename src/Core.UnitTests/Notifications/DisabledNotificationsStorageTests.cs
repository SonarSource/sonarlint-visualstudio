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
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core.VsVersion;

namespace SonarLint.VisualStudio.Core.UnitTests.Notifications
{
    [TestClass]
    public class DisabledNotificationsStorageTests
    {
        [DataRow("2", true)]
        [DataRow("4", false)]
        [TestMethod]
        public void IsNotificationDisabled_ReturnsCorrect(string notificationId, bool expectedResult)
        {
            var file = CreateFileMock("1", "2", "3");
            var testSubject = CreateTestSubject(file.Object);
                        
            var result = testSubject.IsNotificationDisabled(notificationId);

            result.Should().Be(expectedResult);
            file.Verify(f => f.ReadAllLines(GetFilePath()), Times.Once);
        }

        [TestMethod]
        public void DisableNotification_IsNotDisabled_Disables()
        {
            var file = CreateFileMock("1", "2", "3");
            var testSubject = CreateTestSubject(file.Object);

            var result = testSubject.IsNotificationDisabled("4"); 
            result.Should().BeFalse();

            testSubject.DisableNotification("4");

            result = testSubject.IsNotificationDisabled("4");
            
            result.Should().BeTrue();
            file.Verify(f => f.WriteAllLines(GetFilePath(), It.IsAny<IEnumerable<string>>()), Times.Once);
            var disabledNotifications = (IEnumerable<string>)file.Invocations[1].Arguments[1];

            disabledNotifications.Count().Should().Be(4);
            disabledNotifications.Contains("4").Should().BeTrue();
        }

        [TestMethod]
        public void DisableNotification_AlreadyDisabled_DoesNothing()
        {
            var file = CreateFileMock("1", "2", "3");
            var testSubject = CreateTestSubject(file.Object);

            var result = testSubject.IsNotificationDisabled("3");
            result.Should().BeTrue();

            testSubject.DisableNotification("3");

            result = testSubject.IsNotificationDisabled("3");

            result.Should().BeTrue();
            file.Verify(f => f.WriteAllLines(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        private IDisabledNotificationsStorage CreateTestSubject(IFile file)
        {
            var fileSystem = CreateFileSystem(file);
            var versionProvider = CreateVsVersionProvider();

            var testSubject = new DisabledNotificationsStorage(versionProvider, fileSystem);

            return testSubject;
        }

        private IFileSystem CreateFileSystem(IFile file)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.File).Returns(file);

            return fileSystem.Object;
        }

        private Mock<IFile> CreateFileMock(params string[] fileContent)
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllLines(It.IsAny<string>())).Returns(fileContent);

            return file;
        }

        private IVsVersionProvider CreateVsVersionProvider()
        {
            var version = new Mock<IVsVersion>();
            version.SetupGet(v => v.MajorInstallationVersion).Returns("17");

            var versionProvider = new Mock<IVsVersionProvider>();
            versionProvider.SetupGet(vp => vp.Version).Returns(version.Object);

            return versionProvider.Object;
        }

        private string GetFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fullPath = Path.Combine(appData, "SonarLint for Visual Studio", "17", "disabledNotifications.txt");

            return fullPath;
        }
    }


}
