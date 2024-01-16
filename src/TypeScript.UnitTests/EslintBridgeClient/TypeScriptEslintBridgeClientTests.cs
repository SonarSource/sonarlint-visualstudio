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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class TypeScriptEslintBridgeClientTests
    {
        private const int ServerPort = 1234;

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TypeScriptEslintBridgeClient, ITypeScriptEslintBridgeClient>(
                MefTestHelpers.CreateExport<IEslintBridgeProcessFactory>(),
                MefTestHelpers.CreateExport<ILogger>());
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
                ParsingError = new ParsingError { Line = 111, Code = ParsingErrorCode.MISSING_TYPESCRIPT, Message = "a message" }
            };

            var httpWrapper = SetupHttpWrapper("tsconfig-files", JsonConvert.SerializeObject(response));

            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.TsConfigFiles("some path", CancellationToken.None);

            result.Should().BeEquivalentTo(response);
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

        private TypeScriptEslintBridgeClient CreateTestSubject(IEslintBridgeHttpWrapper httpWrapper)
        {
            var eslintBridgeProcess = SetupServerProcess().Object;

            return new TypeScriptEslintBridgeClient(eslintBridgeProcess, httpWrapper, Mock.Of<ILogger>());
        }

        private Mock<IEslintBridgeProcess> SetupServerProcess()
        {
            var serverProcess = new Mock<IEslintBridgeProcess>();
            serverProcess.Setup(x => x.Start()).ReturnsAsync(ServerPort);

            return serverProcess;
        }

        private static Uri BuildServerUri(string endpoint) => new Uri($"http://localhost:{ServerPort}/{endpoint}");
    }
}
