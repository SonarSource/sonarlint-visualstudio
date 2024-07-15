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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Notifications
{
    [TestClass]
    public class DisabledNotificationsStorageTests
    {
        private const string DefaultRootPath = "c:\\users\\someuser";
        private const string HardCodedVSVersion = "17";

        private string ExpectedDefaultDisabledNotificationsFilePath => GetExpectedDisabledNotificationsFilePath();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<DisabledNotificationsStorage, IDisabledNotificationsStorage>(new[]
            {
                MefTestHelpers.CreateExport<IVsInfoProvider>(),
                MefTestHelpers.CreateExport<ILogger>()
            });
        }

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
            file.Verify(f => f.ReadAllText(ExpectedDefaultDisabledNotificationsFilePath), Times.Once);

            //To make sure we do not go to disk on consecutive calls 
            _ = testSubject.IsNotificationDisabled(notificationId);
            file.Verify(f => f.ReadAllText(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void IsNotificationDisabled_FileDoesNotExists_ReturnsFalse()
        {
            var file = CreateFileMock(fileExists: false);

            var testSubject = CreateTestSubject(file.Object);

            var result = testSubject.IsNotificationDisabled("1");

            result.Should().BeFalse();
            file.Verify(f => f.Exists(ExpectedDefaultDisabledNotificationsFilePath), Times.Once);
            file.Verify(f => f.ReadAllText(ExpectedDefaultDisabledNotificationsFilePath), Times.Never);
        }

        [TestMethod]
        public void IsNotificationDisabled_ReadError_LogsError()
        {
            var file = CreateFileMock();
            file.Setup(f => f.ReadAllText(It.IsAny<string>())).Throws(new Exception("this is a test"));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            var result = testSubject.IsNotificationDisabled("1");

            result.Should().BeFalse();
            logger.AssertOutputStrings(2);
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void IsNotificationDisabled_CriticalException_Throws()
        {
            var file = CreateFileMock();
            file.Setup(f => f.ReadAllText(It.IsAny<string>())).Throws(new StackOverflowException("this is a critical error"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            Action act = () => testSubject.IsNotificationDisabled("4");
            act.Should().Throw<StackOverflowException>().WithMessage("this is a critical error");
        }

        [TestMethod]
        public void DisableNotification_IsNotDisabled_Disables()
        {
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);

            var testSubject = CreateTestSubject(file.Object);

            testSubject.DisableNotification("4");
            
            var result = testSubject.IsNotificationDisabled("4");

            result.Should().BeTrue();
            file.Verify(f => f.WriteAllText(ExpectedDefaultDisabledNotificationsFilePath, It.IsAny<string>()), Times.Once);

            var disabledNotificationsJson = (string)file.Invocations[2].Arguments[1];
            var disabledNotificationsResult = JsonConvert.DeserializeObject<NotificationSettings>(disabledNotificationsJson);

            disabledNotificationsResult.DisabledNotifications.Count().Should().Be(4);
            disabledNotificationsResult.DisabledNotifications.Any(n => n.Id == "4").Should().BeTrue();

            //To make sure we do not go to disk on consecutive calls 
            testSubject.DisableNotification("4");
            file.Verify(f => f.ReadAllText(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void DisableNotification_AlreadyDisabled_DoesNothing()
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
        public void DisableNotification_FileDoesNotExists_Disables()
        {
            var file = CreateFileMock(fileExists: false);
            var directory = new Mock<IDirectory>();

            var testSubject = CreateTestSubject(file.Object, directory: directory.Object);

            testSubject.DisableNotification("1");

            var result = testSubject.IsNotificationDisabled("1");

            result.Should().BeTrue();
            file.Verify(f => f.WriteAllText(ExpectedDefaultDisabledNotificationsFilePath, It.IsAny<string>()), Times.Once);
            directory.Verify(d => d.CreateDirectory(Path.GetDirectoryName(ExpectedDefaultDisabledNotificationsFilePath)), Times.Once);

            var disabledNotificationsJson = (string)file.Invocations[1].Arguments[1];
            var disabledNotificationsResult = JsonConvert.DeserializeObject<NotificationSettings>(disabledNotificationsJson);

            disabledNotificationsResult.DisabledNotifications.Count().Should().Be(1);
            disabledNotificationsResult.DisabledNotifications.Any(n => n.Id == "1").Should().BeTrue();
        }

        [TestMethod]
        public void DisableNotification_FileCorrupted_Overrides()
        {
            var file = CreateFileMock();

            var testSubject = CreateTestSubject(file.Object);

            testSubject.DisableNotification("1");

            var result = testSubject.IsNotificationDisabled("1");

            result.Should().BeTrue();
            file.Verify(f => f.WriteAllText(ExpectedDefaultDisabledNotificationsFilePath, It.IsAny<string>()), Times.Once);

            var disabledNotificationsJson = (string)file.Invocations[2].Arguments[1];
            var disabledNotificationsResult = JsonConvert.DeserializeObject<NotificationSettings>(disabledNotificationsJson);

            disabledNotificationsResult.DisabledNotifications.Count().Should().Be(1);
            disabledNotificationsResult.DisabledNotifications.Any(n => n.Id == "1").Should().BeTrue();
        }

        [TestMethod]
        public void DisableNotification_ReadError_LogsError()
        {
            var file = CreateFileMock();
            file.Setup(f => f.ReadAllText(ExpectedDefaultDisabledNotificationsFilePath)).Throws(new Exception("this is a test"));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            testSubject.DisableNotification("1");

            logger.AssertOutputStrings(2);
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void DisableNotification_WriteError_LogsError()
        {
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);
            file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("this is a test"));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(file.Object, logger);

            testSubject.DisableNotification("4");

            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void DisableNotification_CriticalException_Throws()
        {
            var disabledNotifications = CreateDisabledNotifications("1", "2", "3");
            var file = CreateFileMock(disabledNotifications);
            file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new StackOverflowException("this is a critical error"));

            var testSubject = CreateTestSubject(file.Object);

            Action act = () => testSubject.DisableNotification("4");

            act.Should().Throw<StackOverflowException>().WithMessage("this is a critical error");
        }

        private IDisabledNotificationsStorage CreateTestSubject(IFile file, ILogger logger = null, IDirectory directory = null,
            IEnvironmentVariableProvider environmentVars = null)
        {
            logger = logger ?? Mock.Of<ILogger>();

            var fileSystem = CreateFileSystem(file, directory);
            var versionProvider = CreateVsVersionProvider();
            environmentVars ??= CreateEnvironmentVars(DefaultRootPath);

            var testSubject = new DisabledNotificationsStorage(versionProvider, logger, fileSystem, environmentVars);

            return testSubject;
        }

        private IFileSystem CreateFileSystem(IFile file, IDirectory directory)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.File).Returns(file);

            directory = directory ?? Mock.Of<IDirectory>();
            fileSystem.SetupGet(fs => fs.Directory).Returns(directory);

            return fileSystem.Object;
        }

        private Mock<IFile> CreateFileMock(NotificationSettings disabledNotifications = null, bool fileExists = true)
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.Exists(ExpectedDefaultDisabledNotificationsFilePath)).Returns(fileExists);
            if (disabledNotifications != null)
            {
                file.Setup(f => f.ReadAllText(ExpectedDefaultDisabledNotificationsFilePath)).Returns(JsonConvert.SerializeObject(disabledNotifications));
            }
            return file;
        }

        private IVsInfoProvider CreateVsVersionProvider()
        {
            var version = new Mock<IVsVersion>();
            version.SetupGet(v => v.MajorInstallationVersion).Returns(HardCodedVSVersion);

            var versionProvider = new Mock<IVsInfoProvider>();
            versionProvider.SetupGet(vp => vp.Version).Returns(version.Object);

            return versionProvider.Object;
        }

        private static IEnvironmentVariableProvider CreateEnvironmentVars(string rootPath = null)
        {
            var envVars = new Mock<IEnvironmentVariableProvider>();
            envVars.Setup(x => x.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(rootPath ?? DefaultRootPath);
            return envVars.Object;
        }

        private string GetExpectedDisabledNotificationsFilePath()
            => Path.Combine(DefaultRootPath, "SonarLint for Visual Studio", HardCodedVSVersion, "internal.notifications.json");

        private NotificationSettings CreateDisabledNotifications(params string[] ids)
        {
            var disabledNotifications = new NotificationSettings();

            foreach (var id in ids)
            {
                disabledNotifications.AddDisabledNotification(id);
            }

            return disabledNotifications;
        }
    }
}
