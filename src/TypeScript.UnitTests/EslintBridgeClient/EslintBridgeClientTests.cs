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
        private static readonly Uri BaseServerUri = new Uri("http://localhost:123");

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

            httpWrapper.Verify(x => x.PostAsync(BuildFullUri("init-linter"), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task AnalyzeJs_HttpWrapperCalledWithCorrectArguments()
        {
            var httpWrapper = SetupHttpWrapper("analyze-js", assertReceivedRequest: receivedRequest =>
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

            httpWrapper.Verify(x => x.PostAsync(BuildFullUri("analyze-js"), It.IsAny<object>(), token), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task AnalyzeJs_NullResponse_Null()
        {
            var httpWrapper = SetupHttpWrapper("analyze-js", response: null);
            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.AnalyzeJs("some path", CancellationToken.None);

            result.Should().BeNull();
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
                .Setup(x => x.PostAsync(BuildFullUri("analyze-js"), It.IsAny<object>(), It.IsAny<CancellationToken>()))
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
                .Setup(x => x.PostAsync(BuildFullUri("analyze-js"), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new StackOverflowException());

            var testSubject = CreateTestSubject(httpWrapper.Object);

            Func<Task> act = async () => await testSubject.AnalyzeJs("some path", CancellationToken.None);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void Dispose_DisposesHttpWrapper()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            var testSubject = CreateTestSubject(httpWrapper.Object);
            testSubject.Dispose();

            httpWrapper.Verify(x => x.PostAsync(BuildFullUri("close"), null, CancellationToken.None), Times.Once);
            httpWrapper.Verify(x => x.Dispose(), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        private TypeScript.EslintBridgeClient.EslintBridgeClient CreateTestSubject(IEslintBridgeHttpWrapper httpWrapper = null,
            IAnalysisConfiguration analysisConfiguration = null)
        {
            analysisConfiguration ??= Mock.Of<IAnalysisConfiguration>();

            return new TypeScript.EslintBridgeClient.EslintBridgeClient(BaseServerUri, httpWrapper, analysisConfiguration);
        }

        private static Mock<IEslintBridgeHttpWrapper> SetupHttpWrapper(string endpoint, string response = null, Action<object> assertReceivedRequest = null)
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            httpWrapper
                .Setup(x => x.PostAsync(BuildFullUri(endpoint), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Callback((Uri uri, object data, CancellationToken token) => assertReceivedRequest?.Invoke(data))
                .ReturnsAsync(response);

            return httpWrapper;
        }

        private static Uri BuildFullUri(string endpoint) => new Uri(BaseServerUri, endpoint);
    }
}
