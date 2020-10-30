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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Api
{
    [TestClass]
    public class OpenInIDERequestHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var validatorExport = MefTestHelpers.CreateExport<IOpenInIDEStateValidator>(Mock.Of<IOpenInIDEStateValidator>());
            var sonarQubeServiceExport = MefTestHelpers.CreateExport<ISonarQubeService>(Mock.Of<ISonarQubeService>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>());

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<OpenInIDERequestHandler, IOpenInIDERequestHandler>(null, new[] { validatorExport, sonarQubeServiceExport, loggerExport });
        }

        [TestMethod]
        public void ShowHotspot_InvalidArg_Throws()
        {
            IOpenInIDERequestHandler testSubject = new OpenInIDERequestHandler(Mock.Of<IOpenInIDEStateValidator>(), Mock.Of<ISonarQubeService>(), Mock.Of<ILogger>());

            Action act = () => testSubject.ShowHotspot(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("request");
        }

        [TestMethod]
        public void ShowHotpot_IdeNotInCorrectState_NoActionTaken()
        {
            var validatorMock = CreateValidator("http://localhost/", "project", "org", false);
            var request = new TestShowHotspotRequest
            {
                ServerUrl = "http://localhost/",
                ProjectKey = "project",
                HotspotKey = "any",
                OrganizationKey = "org"
            };
            var serverMock = new Mock<ISonarQubeService>();

            IOpenInIDERequestHandler testSubject = new OpenInIDERequestHandler(validatorMock.Object, serverMock.Object, Mock.Of<ILogger>());

            Action act = () => testSubject.ShowHotspot(request);

            act.Should().NotThrow();
            validatorMock.VerifyAll();
            serverMock.Invocations.Should().BeEmpty();
        }

        private static Mock<IOpenInIDEStateValidator> CreateValidator(string uri, string projectKey, string orgKey, bool canHandleRequest)
        {
            var mockValidator = new Mock<IOpenInIDEStateValidator>();
            mockValidator.Setup(x => x.CanHandleOpenInIDERequest(It.Is<Uri>(x => x.ToString() == uri), projectKey, orgKey))
                .Returns(canHandleRequest);

            return mockValidator;
        }

        private class TestShowHotspotRequest : IShowHotspotRequest
        {
            public string ServerUrl { get; set; }
            public string OrganizationKey { get; set; }
            public string ProjectKey { get; set; }
            public string HotspotKey { get; set; }
        }
    }
}
