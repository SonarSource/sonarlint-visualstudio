/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Newtonsoft.Json;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class JsTsEslintBridgeClientBaseTests
    {
        private const int ServerPort = 1234;
        private const string AnalyzeEndpoint = "dummy-analyze";

        [TestMethod]
        public async Task InitLinter_HttpWrapperCalledWithCorrectArguments()
        {
            var analysisConfiguration = new Mock<IAnalysisConfiguration>();
            analysisConfiguration.Setup(x => x.GetEnvironments()).Returns(new[] { "env1", "env2" });
            analysisConfiguration.Setup(x => x.GetGlobals()).Returns(new[] { "global1", "global2" });

            var rules = new[] { new Rule { Key = "test1" }, new Rule { Key = "test2" } };

            var httpWrapper = SetupHttpWrapper("init-linter", assertReceivedRequest: receivedRequest =>
            {
                var initLinterRequest = receivedRequest as InitLinterRequest;
                initLinterRequest.Should().NotBeNull();
                initLinterRequest.Rules.Should().BeEquivalentTo(rules);
                initLinterRequest.Environments.Should().BeEquivalentTo("env1", "env2");
                initLinterRequest.Globals.Should().BeEquivalentTo("global1", "global2");
            });

            var testSubject = CreateTestSubject(httpWrapper.Object, analysisConfiguration.Object);

            var token = new CancellationToken();
            await testSubject.InitLinter(rules, token);

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("init-linter"), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not ok")]
        [DataRow("ok!")] // wrong case
        public void InitLinter_InvalidResponse_InvalidOperationException(string response)
        {
            var analysisConfiguration = new Mock<IAnalysisConfiguration>();
            analysisConfiguration.Setup(x => x.GetEnvironments()).Returns(new[] { "env1", "env2" });
            analysisConfiguration.Setup(x => x.GetGlobals()).Returns(new[] { "global1", "global2" });

            var rules = new[] { new Rule { Key = "test1" }, new Rule { Key = "test2" } };

            var httpWrapper = SetupHttpWrapper("init-linter", response);
            var testSubject = CreateTestSubject(httpWrapper.Object, analysisConfiguration.Object);

            Func<Task> act = async () => await testSubject.InitLinter(rules, CancellationToken.None);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public async Task Analyze_HttpWrapperCalledWithCorrectArguments()
        {
            var httpWrapper = SetupHttpWrapper(AnalyzeEndpoint, JsonConvert.SerializeObject(new JsTsAnalysisResponse()), assertReceivedRequest: receivedRequest =>
            {
                var analysisRequest = receivedRequest as JsTsAnalysisRequest;
                analysisRequest.Should().NotBeNull();
                analysisRequest.IgnoreHeaderComments.Should().BeTrue();
                analysisRequest.FilePath.Should().Be("some path");
                analysisRequest.TSConfigFilePaths.Should().BeEquivalentTo("some config");
                analysisRequest.FileType.Should().Be("MAIN");
            });

            var testSubject = CreateTestSubject(httpWrapper.Object);

            var token = new CancellationToken();
            await testSubject.Analyze("some path", "some config", token);

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri(AnalyzeEndpoint), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void Analyze_EmptyResponse_InvalidOperationException(string response)
        {
            var httpWrapper = SetupHttpWrapper(AnalyzeEndpoint, response);
            var testSubject = CreateTestSubject(httpWrapper.Object);
            Func<Task> act = async () => await testSubject.Analyze("some path", "some config", CancellationToken.None);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public async Task Analyze_HasResponse_DeserializedResponse()
        {
            var analysisResponse = new JsTsAnalysisResponse { Issues = new[] { new Issue { Column = 1, EndColumn = 2 } } };
            var httpWrapper = SetupHttpWrapper(AnalyzeEndpoint, JsonConvert.SerializeObject(analysisResponse));
            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            result.Should().BeEquivalentTo(analysisResponse);
        }

        [TestMethod]
        public void Analyze_FailsToDeserializedResponse_ExceptionThrown()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri(AnalyzeEndpoint), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("invalid json");

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.Analyze("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<JsonReaderException>();
        }

        [TestMethod]
        public void Analyze_CriticalException_ExceptionNotCaught()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri(AnalyzeEndpoint), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new StackOverflowException());

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.Analyze("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        [Description("Regression test for #2469")]
        public void Dispose_EslintBridgeProcessIsNotRunning_DisposesHttpWrapper()
        {
            var eslintBridgeProcess = SetupServerProcess(isRunning: false);
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);
            testSubject.Dispose();

            httpWrapper.Verify(x => x.Dispose(), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_EslintBridgeProcessIsRunning_ClosesAndDisposesHttpWrapper()
        {
            var eslintBridgeProcess = SetupServerProcess(isRunning: true);
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);
            testSubject.Dispose();

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("close"), null, CancellationToken.None), Times.Once);
            httpWrapper.Verify(x => x.Dispose(), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_DisposesEslintBridgeProcess()
        {
            var eslintBridgeProcess = new Mock<IEslintBridgeProcess>();

            var testSubject = CreateTestSubject(eslintBridgeProcess: eslintBridgeProcess.Object);
            testSubject.Dispose();

            eslintBridgeProcess.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Dispose_FailsToCallClose_HttpWrapperStillDisposed()
        {
            var eslintBridgeProcess = SetupServerProcess(isRunning: true);

            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri("close"), null, CancellationToken.None))
                .Throws<NotImplementedException>();

            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);
            testSubject.Dispose();

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("close"), null, CancellationToken.None), Times.Once);
            httpWrapper.Verify(x => x.Dispose(), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_DisposesEslintBridgeKeepAlive()
        {
            var keepAlive = new Mock<IEslintBridgeKeepAlive>();

            var testSubject = CreateTestSubject(keepAlive: keepAlive.Object);
            testSubject.Dispose();

            keepAlive.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task Close_StopsEslintBridgeProcess()
        {
            var eslintBridgeProcess = new Mock<IEslintBridgeProcess>();

            var testSubject = CreateTestSubject(eslintBridgeProcess: eslintBridgeProcess.Object);
            await testSubject.Close();

            eslintBridgeProcess.Verify(x => x.Stop(), Times.Once);
            eslintBridgeProcess.Verify(x => x.Dispose(), Times.Never);
        }

        [TestMethod]
        public async Task Close_EslintBridgeProcessIsNotRunning_DoesNotCallEslintBridgeServer()
        {
            var eslintBridgeProcess = SetupServerProcess(isRunning: false);
            var httpWrapper = SetupHttpWrapper("close");

            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);

            await testSubject.Close();

            httpWrapper.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task Close_EslintBridgeProcessIsRunning_ClosesEslintBridgeServer()
        {
            var eslintBridgeProcess = SetupServerProcess(isRunning: true);
            var httpWrapper = SetupHttpWrapper("close");
            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);
            await testSubject.Close();

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("close"), null, CancellationToken.None), Times.Once);
            httpWrapper.Verify(x => x.Dispose(), Times.Never);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Close_FailsToCallClose_EslintBridgeProcessStillStopped()
        {
            var eslintBridgeProcess = new Mock<IEslintBridgeProcess>();

            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri("close"), null, CancellationToken.None))
                .Throws<NotImplementedException>();

            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);
            await testSubject.Close();

            eslintBridgeProcess.Verify(x => x.Stop(), Times.Once);
            eslintBridgeProcess.Verify(x => x.Dispose(), Times.Never);
        }

        private static JsTsEslintBridgeClientBase CreateTestSubject(IEslintBridgeHttpWrapper httpWrapper = null,
            IAnalysisConfiguration analysisConfiguration = null,
            IEslintBridgeProcess eslintBridgeProcess = null,
            IEslintBridgeKeepAlive keepAlive = null)
        {
            analysisConfiguration ??= Mock.Of<IAnalysisConfiguration>();
            eslintBridgeProcess ??= SetupServerProcess().Object;
            httpWrapper ??= Mock.Of<IEslintBridgeHttpWrapper>();
            keepAlive ??= new Mock<IEslintBridgeKeepAlive>().Object;

            return new JsTsEslintBridgeClientBase(AnalyzeEndpoint, eslintBridgeProcess, httpWrapper, analysisConfiguration,
                keepAlive);
        }

        private static Mock<IEslintBridgeHttpWrapper> SetupHttpWrapper(string endpoint, string response = "OK!", Action<object> assertReceivedRequest = null)
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri(endpoint), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Callback((Uri uri, object data, CancellationToken token) => assertReceivedRequest?.Invoke(data))
                .ReturnsAsync(response);

            return httpWrapper;
        }

        private static Mock<IEslintBridgeProcess> SetupServerProcess(bool isRunning = false)
        {
            var serverProcess = new Mock<IEslintBridgeProcess>();
            serverProcess.Setup(x => x.Start()).ReturnsAsync(ServerPort);
            serverProcess.Setup(x => x.IsRunning).Returns(isRunning);

            return serverProcess;
        }

        private static Uri BuildServerUri(string endpoint) => new Uri($"http://localhost:{ServerPort}/{endpoint}");
    }
}
