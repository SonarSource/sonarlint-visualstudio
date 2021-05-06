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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.TsConfig;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.TsConfig
{
    [TestClass]
    public class TsConfigProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TsConfigProvider, ITsConfigProvider>(null, new[]
            {
                MefTestHelpers.CreateExport<ITsConfigsLocator>(Mock.Of<ITsConfigsLocator>()),
            });
        }

        [TestMethod]
        public async Task GetConfigForFile_NoTsConfigsInSolution_Null()
        {
            var tsConfigsLocator = CreateTsConfigsLocator(Array.Empty<string>());
            var eslintBridgeClient = CreateEslintBridgeClient();
            var testSubject = CreateTestSubject(tsConfigsLocator.Object);

            var result = await testSubject.GetConfigForFile("some file", eslintBridgeClient.Object, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetConfigForFile_SourceFileIsNotInAnyTsConfig_Null()
        {
            var tsConfigsInSolution = new[] {"config1", "config2", "config3"};
            var tsConfigsLocator = CreateTsConfigsLocator(tsConfigsInSolution);

            var eslintBridgeClient = CreateEslintBridgeClient();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1", new TSConfigResponse()},
                {"config2", new TSConfigResponse{Files = new List<string>{"some other file"}}},
                {"config3", new TSConfigResponse{Files = new List<string>{"some file2"}}},
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object);
            var result = await testSubject.GetConfigForFile("some file", eslintBridgeClient.Object, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Verify(x=> x.TsConfigFiles("config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x=> x.TsConfigFiles("config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x=> x.TsConfigFiles("config3", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("tested file")]
        [DataRow("tested FILE")] // case-insensitive
        public async Task GetConfigForFile_SourceFileFoundInTsConfig_OtherTsConfigsNotChecked(string foundFileName)
        {
            var tsConfigsInSolution = new[] { "config1", "config2", "config3" };
            var tsConfigsLocator = CreateTsConfigsLocator(tsConfigsInSolution);

            var eslintBridgeClient = CreateEslintBridgeClient();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1", new TSConfigResponse()},
                {"config2", new TSConfigResponse{Files = new List<string>{foundFileName}}},
                {"config3", new TSConfigResponse()},
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object);
            var result = await testSubject.GetConfigForFile("tested file", eslintBridgeClient.Object, CancellationToken.None);
            result.Should().Be("config2");

            eslintBridgeClient.Verify(x => x.TsConfigFiles("config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config3", CancellationToken.None), Times.Never);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        private static void SetupEslintBridgeResponse(
            Mock<IEslintBridgeClient> eslintBridgeClient,
            IDictionary<string, TSConfigResponse> response)
        {
            foreach (var responseValues in response)
            {
                eslintBridgeClient
                    .Setup(x => x.TsConfigFiles(responseValues.Key, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(responseValues.Value);
            }
        }

        private Mock<ITsConfigsLocator> CreateTsConfigsLocator(IReadOnlyList<string> tsConfigsInSolution)
        {
            var tsConfigsLocator = new Mock<ITsConfigsLocator>();
            tsConfigsLocator.Setup(x => x.Locate()).Returns(tsConfigsInSolution);

            return tsConfigsLocator;
        }

        private Mock<IEslintBridgeClient> CreateEslintBridgeClient()
        {
            var eslintBridgeClient = new Mock<IEslintBridgeClient>();

            return eslintBridgeClient;
        }

        private TsConfigProvider CreateTestSubject(ITsConfigsLocator tsConfigsLocator)
        {
            return new TsConfigProvider(tsConfigsLocator);
        }
    }
}
