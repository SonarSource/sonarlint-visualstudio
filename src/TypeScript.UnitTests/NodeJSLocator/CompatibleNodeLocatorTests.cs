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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.JsTs;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator;
using SonarLint.VisualStudio.TypeScript.Notifications;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator
{
    [TestClass]
    public class CompatibleNodeLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CompatibleNodeLocator, ICompatibleNodeLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<INodeVersionInfoProvider>(Mock.Of<INodeVersionInfoProvider>()),
                MefTestHelpers.CreateExport<IUnsupportedNodeVersionNotificationService>(Mock.Of<IUnsupportedNodeVersionNotificationService>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void Locate_NoDetectedVersions_Null()
        {
            var testSubject = CreateTestSubject();
            var result = testSubject.Locate();

            result.Should().BeNull();
        }

        [TestMethod]
        public void Locate_NoCompatibleVersions_ReturnsNull()
        {
            var versions = new List<NodeVersionInfo>
            {
                new("bad version", new Version(9, 0)),
                new("bad version", new Version(11, 0))
            };

            var testSubject = CreateTestSubject(versions);

            var result = testSubject.Locate();
            result.Should().BeNull();
        }

        [TestMethod]
        public void Locate_ReturnsFirstCompatiblePath()
        {
            var versions = new List<NodeVersionInfo>
            {
                new("bad version", new Version(11, 0)),
                new("compatible1", new Version(12, 0)),
                new("compatible2", new Version(12, 0)),
            };

            var testSubject = CreateTestSubject(versions);

            var result = testSubject.Locate();
            result.Should().Be(versions[1]);
        }

        [TestMethod]
        public void Locate_NoCompatibleVersions_NotificationIsShown()
        {
            var versions = new List<NodeVersionInfo>
            {
                new("bad version", new Version(9, 0))
            };

            var notificationService = new Mock<IUnsupportedNodeVersionNotificationService>();
            var testSubject = CreateTestSubject(versions, notificationService.Object);

            _ = testSubject.Locate();
            notificationService.Verify(x => x.Show(), Times.Once);
            notificationService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_CompatibleVersionFound_NotificationIsNotShown()
        {
            var versions = new List<NodeVersionInfo>
            {
                new("bad version", new Version(12, 0))
            };

            var notificationService = new Mock<IUnsupportedNodeVersionNotificationService>();
            var testSubject = CreateTestSubject(versions, notificationService.Object);

            _ = testSubject.Locate();
            notificationService.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(9, false)]
        [DataRow(10, true)]
        [DataRow(11, false)]
        [DataRow(12, true)]
        [DataRow(13, true)]
        public void IsCompatibleVersion_ReturnsTrueFalse(int majorVersion, bool expectedResult)
        {
            var version = new Version(majorVersion, 0);
            var result = CompatibleNodeLocator.IsCompatibleVersion(version);

            result.Should().Be(expectedResult);
        }

        private CompatibleNodeLocator CreateTestSubject(IReadOnlyCollection<NodeVersionInfo> candidateLocations = null,
            IUnsupportedNodeVersionNotificationService unsupportedNodeNotificationService = null)
        {
            candidateLocations ??= Array.Empty<NodeVersionInfo>();
            var versionInfoProvider = new Mock<INodeVersionInfoProvider>();
            versionInfoProvider.Setup(x => x.GetAllNodeVersions()).Returns(candidateLocations);

            unsupportedNodeNotificationService ??= Mock.Of<IUnsupportedNodeVersionNotificationService>();

            var logger = Mock.Of<ILogger>();

            return new CompatibleNodeLocator(versionInfoProvider.Object, unsupportedNodeNotificationService, logger);
        }
    }
}
