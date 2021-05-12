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
    public class EslintBridgeClientTests
    {
        private const int ServerPort = 1234;

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
        public async Task AnalyzeJs_HttpWrapperCalledWithCorrectArguments()
        {
            var httpWrapper = SetupHttpWrapper("analyze-js", JsonConvert.SerializeObject(new AnalysisResponse()), assertReceivedRequest: receivedRequest =>
            {
                var analysisRequest = receivedRequest as AnalysisRequest;
                analysisRequest.Should().NotBeNull();
                analysisRequest.IgnoreHeaderComments.Should().BeTrue();
                analysisRequest.FilePath.Should().Be("some path");
                analysisRequest.TSConfigFilePaths.Should().BeEmpty();
            });

            var testSubject = CreateTestSubject(httpWrapper.Object);

            var token = new CancellationToken();
            await testSubject.AnalyzeJs("some path", token);

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("analyze-js"), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void AnalyzeJs_EmptyResponse_InvalidOperationException(string response)
        {
            var httpWrapper = SetupHttpWrapper("analyze-js", response);
            var testSubject = CreateTestSubject(httpWrapper.Object);
            Func<Task> act = async () => await testSubject.AnalyzeJs("some path", CancellationToken.None);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public async Task AnalyzeJs_HasResponse_DeserializedResponse()
        {
            var analysisResponse = new AnalysisResponse { Issues = new[] { new Issue { Column = 1, EndColumn = 2 } } };
            var httpWrapper = SetupHttpWrapper("analyze-js", JsonConvert.SerializeObject(analysisResponse));
            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.AnalyzeJs("some path", CancellationToken.None);

            result.Should().BeEquivalentTo(analysisResponse);
        }

        [TestMethod]
        public void AnalyzeJs_FailsToDeserializedResponse_ExceptionThrown()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri("analyze-js"), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("invalid json");

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.AnalyzeJs("some path", CancellationToken.None);
            act.Should().ThrowExactly<JsonReaderException>();
        }

        [TestMethod]
        public void AnalyzeJs_CriticalException_ExceptionNotCaught()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri("analyze-js"), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new StackOverflowException());

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.AnalyzeJs("some path", CancellationToken.None);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void AnalyzeJs_NewEslintBridgeProcess_EslintBridgeClientNotInitializedException()
        {
            var eslintBridgeProcess = SetupServerProcess(isNewProcess: true);
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);

            Func<Task> act = async () => await testSubject.AnalyzeJs("some path", CancellationToken.None);
            act.Should().ThrowExactly<EslintBridgeClientNotInitializedException>();
            httpWrapper.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task NewTsConfig_HttpWrapperCalledWithCorrectArguments()
        {
            var httpWrapper = SetupHttpWrapper("new-tsconfig",
                response: "OK!",
                assertReceivedRequest: receivedRequest => receivedRequest.Should().BeNull());

            var testSubject = CreateTestSubject(httpWrapper.Object);

            var token = new CancellationToken();
            await testSubject.NewTsConfig(token);

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("new-tsconfig"), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NewTsConfig_IncorrectResponse_Throws()
        {
            var httpWrapper = SetupHttpWrapper("new-tsconfig",
                response: "not ok");

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.NewTsConfig(new CancellationToken());

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public async Task TSConfigFiles_HttpWrapperCalledWithCorrectArguments()
        {
            var httpWrapper = SetupHttpWrapper("tsconfig-files",
                response: JsonConvert.SerializeObject(new TSConfigResponse()),
                assertReceivedRequest: receivedRequest =>
                {
                    var tsConfigRequest = receivedRequest as TsConfigRequest;
                    tsConfigRequest.Should().NotBeNull();
                    tsConfigRequest.TsConfig.Should().Be("some path");
                });
            var token = new CancellationToken();

            var testSubject = CreateTestSubject(httpWrapper.Object);
            await testSubject.TsConfigFiles("some path", token);

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("tsconfig-files"), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void TSConfigFiles_EmptyResponse_InvalidOperationException(string response)
        {
            var httpWrapper = SetupHttpWrapper("tsconfig-files", response);
            var testSubject = CreateTestSubject(httpWrapper.Object);
            Func<Task> act = async () => await testSubject.TsConfigFiles("some path", CancellationToken.None);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public async Task TSConfigFiles_HasResponse_DeserializedResponse()
        {
            var response = new TSConfigResponse
            {
                Files = new[] { "file1", "file2" },
                ProjectReferences = new[] { "ref1", "ref2" },
                Error = "error",
                ParsingError = new ParsingError { Line = 111,  Code = ParsingErrorCode.MISSING_TYPESCRIPT, Message = "a message" }
            };

            var httpWrapper = SetupHttpWrapper("tsconfig-files", JsonConvert.SerializeObject(response));

            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.TsConfigFiles("some path", CancellationToken.None);

            result.Should().BeEquivalentTo(response);
        }

        [TestMethod]
        public async Task AnalyzeTs_HttpWrapperCalledWithCorrectArguments()
        {
            var httpWrapper = SetupHttpWrapper("analyze-ts", JsonConvert.SerializeObject(new AnalysisResponse()), assertReceivedRequest: receivedRequest =>
            {
                var analysisRequest = receivedRequest as AnalysisRequest;
                analysisRequest.Should().NotBeNull();
                analysisRequest.IgnoreHeaderComments.Should().BeTrue();
                analysisRequest.FilePath.Should().Be("some path");
                analysisRequest.TSConfigFilePaths.Should().BeEquivalentTo("some config");
            });

            var testSubject = CreateTestSubject(httpWrapper.Object);

            var token = new CancellationToken();
            await testSubject.AnalyzeTs("some path", "some config", token);

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("analyze-ts"), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void AnalyzeTs_EmptyResponse_InvalidOperationException(string response)
        {
            var httpWrapper = SetupHttpWrapper("analyze-ts", response);
            var testSubject = CreateTestSubject(httpWrapper.Object);
            Func<Task> act = async () => await testSubject.AnalyzeTs("some path", "some config", CancellationToken.None);

            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public async Task AnalyzeTs_HasResponse_DeserializedResponse()
        {
            var analysisResponse = new AnalysisResponse { Issues = new[] { new Issue { Column = 1, EndColumn = 2 } } };
            var httpWrapper = SetupHttpWrapper("analyze-ts", JsonConvert.SerializeObject(analysisResponse));
            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.AnalyzeTs("some path", "some config", CancellationToken.None);

            result.Should().BeEquivalentTo(analysisResponse);
        }

        [TestMethod]
        public void AnalyzeTs_FailsToDeserializedResponse_ExceptionThrown()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri("analyze-ts"), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("invalid json");

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.AnalyzeTs("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<JsonReaderException>();
        }

        [TestMethod]
        public void AnalyzeTs_CriticalException_ExceptionNotCaught()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri("analyze-ts"), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new StackOverflowException());

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.AnalyzeTs("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void AnalyzeTs_NewEslintBridgeProcess_EslintBridgeClientNotInitializedException()
        {
            var eslintBridgeProcess = SetupServerProcess(isNewProcess: true);
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            var testSubject = CreateTestSubject(httpWrapper.Object, eslintBridgeProcess: eslintBridgeProcess.Object);

            Func<Task> act = async () => await testSubject.AnalyzeTs("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<EslintBridgeClientNotInitializedException>();
            httpWrapper.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Dispose_DisposesHttpWrapper()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            var testSubject = CreateTestSubject(httpWrapper.Object);
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
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri("close"), null, CancellationToken.None))
                .Throws<NotImplementedException>();

            var testSubject = CreateTestSubject(httpWrapper.Object);
            testSubject.Dispose();

            httpWrapper.Verify(x => x.PostAsync(BuildServerUri("close"), null, CancellationToken.None), Times.Once);
            httpWrapper.Verify(x => x.Dispose(), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        private TypeScript.EslintBridgeClient.EslintBridgeClient CreateTestSubject(IEslintBridgeHttpWrapper httpWrapper = null,
            IAnalysisConfiguration analysisConfiguration = null,
            IEslintBridgeProcess eslintBridgeProcess = null)
        {
            analysisConfiguration ??= Mock.Of<IAnalysisConfiguration>();
            eslintBridgeProcess ??= SetupServerProcess().Object;

            return new TypeScript.EslintBridgeClient.EslintBridgeClient(eslintBridgeProcess, httpWrapper, analysisConfiguration);
        }

        private static Mock<IEslintBridgeHttpWrapper> SetupHttpWrapper(string endpoint, string response = null, Action<object> assertReceivedRequest = null)
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            httpWrapper
                .Setup(x => x.PostAsync(BuildServerUri(endpoint), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Callback((Uri uri, object data, CancellationToken token) => assertReceivedRequest?.Invoke(data))
                .ReturnsAsync(response);

            return httpWrapper;
        }

        private Mock<IEslintBridgeProcess> SetupServerProcess(bool isNewProcess = false)
        {
            var serverProcess = new Mock<IEslintBridgeProcess>();
            serverProcess.Setup(x => x.Start()).ReturnsAsync(new EslintBridgeProcessStartResult(ServerPort, isNewProcess));

            return serverProcess;
        }

        private static Uri BuildServerUri(string endpoint) => new Uri($"http://localhost:{ServerPort}/{endpoint}");
    }
}
