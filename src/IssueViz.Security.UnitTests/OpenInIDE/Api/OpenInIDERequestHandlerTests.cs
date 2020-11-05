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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Api
{
    [TestClass]
    public class OpenInIDERequestHandlerTests_MEF
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var validatorExport = MefTestHelpers.CreateExport<IOpenInIDEStateValidator>(Mock.Of<IOpenInIDEStateValidator>());
            var sonarQubeServiceExport = MefTestHelpers.CreateExport<ISonarQubeService>(Mock.Of<ISonarQubeService>());
            var converterExport = MefTestHelpers.CreateExport<IHotspotToIssueVisualizationConverter>(Mock.Of<IHotspotToIssueVisualizationConverter>());
            var navigatorExport = MefTestHelpers.CreateExport<ILocationNavigator>(Mock.Of<ILocationNavigator>());
            var storeExport = MefTestHelpers.CreateExport<IHotspotsStore>(Mock.Of<IHotspotsStore>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>());

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<OpenInIDERequestHandler, IOpenInIDERequestHandler>(null,
                new[] { validatorExport, sonarQubeServiceExport, converterExport, navigatorExport, storeExport, loggerExport });
        }
    }

    [TestClass]
    public class OpenInIDERequestHandlerTests
    {
        private readonly IShowHotspotRequest ValidRequest = new TestShowHotspotRequest
            {
                ServerUrl = new Uri("http://localhost/"),
                ProjectKey = "project",
                HotspotKey = "any",
                OrganizationKey = "org"
            };

        private readonly SonarQubeHotspot ValidServerHotspot = new SonarQubeHotspot("hotspotKey", null, null, null, null, null, null, null, null, null, null, null, null, null);

        private Mock<IOpenInIDEStateValidator> stateValidatorMock;
        private Mock<ISonarQubeService> serverMock;
        private Mock<IHotspotToIssueVisualizationConverter> converterMock;
        private Mock<ILocationNavigator> navigatorMock;
        private Mock<IHotspotsStore> storeMock;
        private TestLogger logger;

        private IOpenInIDERequestHandler testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            stateValidatorMock = new Mock<IOpenInIDEStateValidator>();
            serverMock = new Mock<ISonarQubeService>();
            converterMock = new Mock<IHotspotToIssueVisualizationConverter>();
            navigatorMock = new Mock<ILocationNavigator>();
            storeMock = new Mock<IHotspotsStore>();
            logger = new TestLogger(logToConsole: true);

            testSubject = new OpenInIDERequestHandler(stateValidatorMock.Object, serverMock.Object, converterMock.Object,
                navigatorMock.Object, storeMock.Object, logger);
        }

        [TestMethod]
        public async Task ShowHotspot_InvalidArg_Throws()
        {
            Func<Task> act = async () => await testSubject.ShowHotspotAsync(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("request");
        }

        [TestMethod]
        public async Task ShowHotpot_IdeNotInCorrectState_NoFurtherProcessing()
        {
            InitializeStateValidator(ValidRequest, false);

            // Act
            await testSubject.ShowHotspotAsync(ValidRequest)
                .ConfigureAwait(false);

            CheckCalled(stateValidatorMock);
            CheckNotCalled(serverMock, converterMock, navigatorMock, storeMock);
        }

        [TestMethod]
        public async Task ShowHotspot_FailedToRetrieveHotspotData_NoFurtherProcessing()
        {
            InitializeStateValidator(ValidRequest, true);
            SetServerResponse(ValidRequest, null);

            // Act
            await testSubject.ShowHotspotAsync(ValidRequest)
                .ConfigureAwait(false);

            CheckCalled(stateValidatorMock, serverMock);
            CheckNotCalled(converterMock, navigatorMock, storeMock);
        }

        [TestMethod]
        public async Task ShowHotspot_ConversionFailed_NoFurtherProcessing()
        {
            InitializeStateValidator(ValidRequest, true);
            SetServerResponse(ValidRequest, ValidServerHotspot);

            // Act
            await testSubject.ShowHotspotAsync(ValidRequest)
                .ConfigureAwait(false);

            CheckCalled(stateValidatorMock, serverMock, converterMock);
            CheckNotCalled(navigatorMock, storeMock);
        }

        [TestMethod]
        public async Task ShowHotspot_DataIsValid_NavigationSucceeded_And_IssueAddedToStore()
        {
            const string hotspotFilePath = "c:\\foo\\myFile.txt";
            const int hotspotStartLine = 999;
            var hotspotViz = CreateHotspotVisualization(hotspotFilePath, hotspotStartLine);

            InitializeStateValidator(ValidRequest, true);
            SetServerResponse(ValidRequest, ValidServerHotspot);
            SetConversionResponse(ValidServerHotspot, hotspotViz);
            SetNavigationRespone(hotspotViz, true);
            SetStoreExpectedItem(hotspotViz);

            // Act
            await testSubject.ShowHotspotAsync(ValidRequest)
                .ConfigureAwait(false);

            CheckCalled(stateValidatorMock, serverMock, converterMock, navigatorMock, storeMock);
            // Not expecting an output window message in the success case
            logger.AssertPartialOutputStringDoesNotExist(hotspotFilePath, hotspotStartLine.ToString());
        }

        [TestMethod]
        public async Task ShowHotspot_DataIsValid_NavigationFailed_IssueStillAddedToStore()
        {
            const string hotspotFilePath = "c:\\xx\\yyy.txt";
            const int hotspotStartLine = -12345;
            var hotspotViz = CreateHotspotVisualization(hotspotFilePath, hotspotStartLine);

            InitializeStateValidator(ValidRequest, true);
            SetServerResponse(ValidRequest, ValidServerHotspot);
            SetConversionResponse(ValidServerHotspot, hotspotViz);
            SetNavigationRespone(hotspotViz, false);
            SetStoreExpectedItem(hotspotViz);

            // Act
            await testSubject.ShowHotspotAsync(ValidRequest)
                .ConfigureAwait(false);

            CheckCalled(stateValidatorMock, serverMock, converterMock, navigatorMock, storeMock);
            logger.AssertPartialOutputStringExists(hotspotFilePath, hotspotStartLine.ToString());
        }

        private static IAnalysisIssueVisualization CreateHotspotVisualization(string filePath, int startLine)
        {
            var vizMock = new Mock<IAnalysisIssueVisualization>();
            // Note: these properties are not marked as Verifiable() since they won't always be read
            vizMock.Setup(x => x.FilePath).Returns(filePath);
            vizMock.Setup(x => x.StartLine).Returns(startLine);
            return vizMock.Object;
        }

        private void InitializeStateValidator(IShowHotspotRequest expected, bool canHandleRequest) =>
            stateValidatorMock.Setup(x => x.CanHandleOpenInIDERequest(expected.ServerUrl, expected.ProjectKey, expected.OrganizationKey))
                .Returns(canHandleRequest)
                .Verifiable();

        private void SetServerResponse(IShowHotspotRequest expected, SonarQubeHotspot response) =>
            serverMock.Setup(x => x.GetHotspotAsync(expected.HotspotKey, It.IsAny<CancellationToken>()))
                .Returns(Task<SonarQubeHotspot>.FromResult(response))
                .Verifiable();

        private void SetConversionResponse(SonarQubeHotspot expected, IAnalysisIssueVisualization response) =>
            converterMock.Setup(x => x.Convert(expected)).Returns(response).Verifiable();

        private void SetNavigationRespone(IAnalysisIssueVisualization expected, bool response) =>
            navigatorMock.Setup(x => x.TryNavigate(expected)).Returns(response).Verifiable();

        private void SetStoreExpectedItem(IAnalysisIssueVisualization expected) =>
            storeMock.Setup(x => x.Add(expected)).Verifiable();

        private static void CheckCalled(params Mock[] mocks)
        {
            foreach (var mock in mocks)
            {
                Console.WriteLine($"Checking mock was called: {mock.Object.GetType()}");
                // Note: we're calling Verify() rather than VerifyAll() so we only verify methods
                // marked as Verifiable(). This is to prevent assertions on the IAnalysisIssueVisualization
                // mock if the properties aren't accessed.
                mock.Verify();
                mock.Invocations.Count.Should().Be(1);
            }
        }

        private static void CheckNotCalled(params Mock[] mocks)
        {
            foreach(var mock in mocks)
            {
                Console.WriteLine($"Checking mock wasn't called: {mock.Object.GetType()}");
                mock.Invocations.Should().BeEmpty();
            }
        }

        private class TestShowHotspotRequest : IShowHotspotRequest
        {
            public Uri ServerUrl { get; set; }
            public string OrganizationKey { get; set; }
            public string ProjectKey { get; set; }
            public string HotspotKey { get; set; }
        }
    }
}
