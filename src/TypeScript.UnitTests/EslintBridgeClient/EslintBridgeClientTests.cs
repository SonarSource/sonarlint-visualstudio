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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class EslintBridgeClientTests
    {
        [TestMethod]
        public async Task AnalyzeJs_HttpWrapperCalledWithCorrectArguments()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync("analyze-js", It.IsAny<object>()))
                .ReturnsAsync((string)null);

            var tsConfigPathProvider = new Mock<IDefaultTsConfigPathProvider>();
            tsConfigPathProvider.Setup(x => x.GetFilePath()).Returns("ts config path");

            var testSubject = CreateTestSubject(httpWrapper.Object, tsConfigPathProvider.Object);
            await testSubject.AnalyzeJs("some path");

            httpWrapper.Verify(x => x.PostAsync("analyze-js",
                    It.Is((AnalysisRequest req) =>
                        req.IgnoreHeaderComments &&
                        req.FilePath == "some path" &&
                        req.TSConfigFilePaths.Length == 1 &&
                        req.TSConfigFilePaths[0] == "ts config path")),
                Times.Once);

            httpWrapper.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task AnalyzeJs_NullResponse_Null()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync("analyze-js", It.IsAny<object>()))
                .ReturnsAsync((string) null);

            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.AnalyzeJs("some path");

            result.Should().BeNull();
        }

        [TestMethod] 
        public async Task AnalyzeJs_HasResponse_DeserializedResponse()
        {
            var analysisResponse = new AnalysisResponse {Issues = new[] {new Issue {Column = 1, EndColumn = 2}}};

            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync("analyze-js", It.IsAny<object>()))
                .ReturnsAsync(JsonConvert.SerializeObject(analysisResponse));

            var testSubject = CreateTestSubject(httpWrapper.Object);
            var result = await testSubject.AnalyzeJs("some path");

            result.Should().BeEquivalentTo(analysisResponse);
        }

        [TestMethod]
        public void AnalyzeJs_FailsToDeserializedResponse_ExceptionCaughtAndLogged()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();
            httpWrapper
                .Setup(x => x.PostAsync("analyze-js", It.IsAny<object>()))
                .ReturnsAsync("invalid json");

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(httpWrapper.Object, logger: logger);
            Func<Task> act = async () => await testSubject.AnalyzeJs("some path");
            act.Should().NotThrow();

            logger.AssertPartialOutputStringExists("JsonReaderException");
        }

        [TestMethod]
        public void AnalyzeJs_FailsToRetrieveTsConfig_ExceptionCaughtAndLogged()
        {
            var configPathProvider = new Mock<IDefaultTsConfigPathProvider>();
            configPathProvider
                .Setup(x => x.GetFilePath())
                .Throws(new NotImplementedException("some exception"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(configPathProvider: configPathProvider.Object, logger: logger);

            Func<Task> act = async () => await testSubject.AnalyzeJs("some path");
            act.Should().NotThrow();

            logger.AssertPartialOutputStringExists("some exception");
        }

        [TestMethod]
        public void AnalyzeJs_CriticalException_ExceptionNotCaught()
        {
            var configPathProvider = new Mock<IDefaultTsConfigPathProvider>();
            configPathProvider
                .Setup(x => x.GetFilePath())
                .Throws(new StackOverflowException());

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(configPathProvider: configPathProvider.Object, logger: logger);

            Func<Task> act = async () => await testSubject.AnalyzeJs("some path");
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void Dispose_DisposesHttpWrapper()
        {
            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            var testSubject = CreateTestSubject(httpWrapper.Object);
            testSubject.Dispose();

            httpWrapper.Verify(x=> x.PostAsync("close", null), Times.Once);
            httpWrapper.Verify(x=> x.Dispose(), Times.Once);
            httpWrapper.VerifyNoOtherCalls();
        }

        private TypeScript.EslintBridgeClient.EslintBridgeClient CreateTestSubject(IEslintBridgeHttpWrapper httpWrapper = null,
            IDefaultTsConfigPathProvider configPathProvider = null,
            ILogger logger = null)
        {
            configPathProvider ??= Mock.Of<IDefaultTsConfigPathProvider>();
            logger ??= Mock.Of<ILogger>();

            return new TypeScript.EslintBridgeClient.EslintBridgeClient(httpWrapper, configPathProvider, logger);
        }
    }
}
