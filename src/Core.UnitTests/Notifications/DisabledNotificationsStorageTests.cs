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
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;

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
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);

            var testSubject = CreateTestSubject(file.Object);
                        
            var result = testSubject.IsNotificationDisabled(notificationId);

            result.Should().Be(expectedResult);
            file.Verify(f => f.ReadAllText(GetFilePath()), Times.Once);
        }

        [TestMethod]
        public void IsNotificationDisabled_ReadError_LogsError()
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(It.IsAny<string>())).Throws(new Exception("Test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            var result = testSubject.IsNotificationDisabled("1");

            result.Should().BeFalse();

            logger.AssertOutputStrings(2);
            logger.AssertOutputStringExists("Failed to read disabled notification config from disk. Error:Test");
            logger.AssertOutputStringExists("Couldn't find disabled notification config. Notification will be showed");
        }

        [TestMethod]
        public void DisableNotification_IsNotDisabled_IsDisabledNotCalled_Disables()
        {
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);

            var testSubject = CreateTestSubject(file.Object);

            testSubject.DisableNotification("4");

            var result = testSubject.IsNotificationDisabled("4");

            result.Should().BeTrue();
            file.Verify(f => f.WriteAllText(GetFilePath(), It.IsAny<string>()), Times.Once);

            var disabledNotificationsJson = (string)file.Invocations[1].Arguments[1];
            var disabledNotificationsResult = JsonConvert.DeserializeObject<DisabledNotifications>(disabledNotificationsJson);

            disabledNotificationsResult.Notifications.Count().Should().Be(4);
            disabledNotificationsResult.Notifications.Any(n => n.Id == "4").Should().BeTrue();
        }

        [TestMethod]
        public void DisableNotification_AlreadyDisabled_IsDisabledNotCalled_DoesNothing()
        {
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);
            var testSubject = CreateTestSubject(file.Object);

            testSubject.DisableNotification("3");

            var result = testSubject.IsNotificationDisabled("3");

            result.Should().BeTrue();
            file.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void DisableNotification_IsNotDisabled_IsDisabledCalled_Disables()
        {
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);

            var testSubject = CreateTestSubject(file.Object);

            var result = testSubject.IsNotificationDisabled("4"); 
            result.Should().BeFalse();

            testSubject.DisableNotification("4");

            result = testSubject.IsNotificationDisabled("4");
            
            result.Should().BeTrue();
            file.Verify(f => f.WriteAllText(GetFilePath(), It.IsAny<string>()), Times.Once);
            
            var disabledNotificationsJson = (string)file.Invocations[1].Arguments[1];
            var disabledNotificationsResult = JsonConvert.DeserializeObject<DisabledNotifications>(disabledNotificationsJson);

            disabledNotificationsResult.Notifications.Count().Should().Be(4);
            disabledNotificationsResult.Notifications.Any(n => n.Id == "4").Should().BeTrue();
        }

        [TestMethod]
        public void DisableNotification_AlreadyDisabled_IsDisabledCalled_DoesNothing()
        {
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);
            var testSubject = CreateTestSubject(file.Object);

            var result = testSubject.IsNotificationDisabled("3");
            result.Should().BeTrue();

            testSubject.DisableNotification("3");

            result = testSubject.IsNotificationDisabled("3");

            result.Should().BeTrue();
            file.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private IDisabledNotificationsStorage CreateTestSubject(IFile file, ILogger logger = null)
        {
            logger = logger ?? Mock.Of<ILogger>();

            var fileSystem = CreateFileSystem(file);
            var versionProvider = CreateVsVersionProvider();

            var testSubject = new DisabledNotificationsStorage(versionProvider, logger, fileSystem);

            return testSubject;
        }

        private IFileSystem CreateFileSystem(IFile file)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.File).Returns(file);

            return fileSystem.Object;
        }

        private Mock<IFile> CreateFileMock(DisabledNotifications disabledNotifications)
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(JsonConvert.SerializeObject(disabledNotifications));

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

        private DisabledNotifications CreateDisabledNotifications(params string[] ids)
        {
            var disabledNotifications = new DisabledNotifications();

            foreach (var id in ids)
            {
                disabledNotifications.AddNotification(id);
            }

            return disabledNotifications;
        }
    }
}
