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
using SonarLint.VisualStudio.Integration;
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
                MefTestHelpers.CreateExport<ITypeScriptEslintBridgeClient>(Mock.Of<ITypeScriptEslintBridgeClient>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public async Task GetConfigForFile_NoTsConfigsInSolution_Null()
        {
            const string testedFileName = "some file";
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, Array.Empty<string>());
            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);

            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().BeNull();

            tsConfigsLocator.VerifyAll();
            eslintBridgeClient.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetConfigForFile_SourceFileIsNotInAnyTsConfig_Null()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "config1.json", "config2.json", "config3.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1.json", new TSConfigResponse()},
                {"config2.json", new TSConfigResponse{Files = new List<string>{"some other file"}}},
                {"config3.json", new TSConfigResponse{Files = new List<string>{"some file2"}}},
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Verify(x => x.TsConfigFiles("config1.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config3.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("tested\\file")] // exact match
        [DataRow("tested\\FILE")] // case-insensitive
        [DataRow("tested/FILE")] // different slashes
        public async Task GetConfigForFile_SourceFileFoundInTsConfig_OtherTsConfigsNotChecked(string foundFileName)
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "config1.json", "config2.json", "config3.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1.json", new TSConfigResponse()},
                {"config2.json", new TSConfigResponse{Files = new List<string>{foundFileName}}},
                {"config3.json", new TSConfigResponse()},
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("config2.json");

            eslintBridgeClient.Verify(x => x.TsConfigFiles("config1.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config3.json", CancellationToken.None), Times.Never);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigContainsFileWithIllegalCharacters_FileInTsConfigIsSkipped()
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "config.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config.json", new TSConfigResponse{Files = new List<string>
                {
                    "validPath",
                    "invalid\\*",
                    testedFileName
                }}}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("config.json");
        }

        [TestMethod]
        public async Task GetConfigForFile_FailedToProcessTsConfig_TsConfigIsSkipped()
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "config1.json", "config2.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {
                    "config1.json", new TSConfigResponse
                    {
                        Error = "some error",
                        Files = new[] {testedFileName} // should be ignored
                    }
                },
                {"config2.json", new TSConfigResponse()}
            });

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object, logger);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Verify(x => x.TsConfigFiles("config1.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();

            logger.AssertPartialOutputStringExists("some error");
        }

        [TestMethod]
        public async Task GetConfigForFile_FailedToProcessTsConfig_ParsingError_TsConfigIsSkipped()
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "config1.json", "config2.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            var parsingError = new ParsingError { Code = ParsingErrorCode.UNSUPPORTED_TYPESCRIPT, Message = "some message", Line = 5555 };
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {
                    "config1.json", new TSConfigResponse
                    {
                        ParsingError = parsingError,
                        Files = new[] {testedFileName} // should be ignored
                    }
                },
                {"config2.json", new TSConfigResponse()}
            });

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object, logger);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Verify(x => x.TsConfigFiles("config1.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();

            logger.AssertPartialOutputStringExists(parsingError.Message, parsingError.Code.ToString(), parsingError.Line.ToString());
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasProjectReferences_ProjectReferencesAreChecked()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "config1.json", "config2.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1.json", new TSConfigResponse()},
                {"config2.json", new TSConfigResponse
                {
                    Files = new List<string>{"some other file"},
                    ProjectReferences = new List<string>{ "config3.json" }

                }},
                {"config3.json", new TSConfigResponse{Files = new List<string>{testedFileName}}}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("config3.json");

            eslintBridgeClient.Verify(x => x.TsConfigFiles("config1.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config3.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasProjectReferences_ProjectReferencesAreCheckedBeforeFiles()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "config1.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1.json", new TSConfigResponse
                {
                    Files = new List<string>{testedFileName},
                    ProjectReferences = new List<string>{ "config2.json" }
                }},
                {"config2.json", new TSConfigResponse
                {
                    Files = new List<string>{testedFileName}
                }}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("config2.json");

            // Veify that project references are checked before "TSConfigResponse.Files"
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2.json", CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasProjectReferences_CircularReferencesAreIgnored()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "config1.json", "config3.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1.json", new TSConfigResponse{ProjectReferences = new List<string>{"config2.json"}}},
                {"config2.json", new TSConfigResponse
                {
                    Files = new List<string>{"some other file"},
                    ProjectReferences = new List<string>{ "config1.json" } // circular loop

                }},
                {"config3.json", new TSConfigResponse{Files = new List<string>{testedFileName}}}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("config3.json");

            eslintBridgeClient.Verify(x => x.TsConfigFiles("config1.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config2.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("config3.json", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasFolderProjectReference_ReferenceProcessedWithDefaultTsConfigName()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "config1.json" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"config1.json", new TSConfigResponse
                {
                    ProjectReferences = new List<string>{"c:/a/b"}
                }},
                {"c:\\a\\b\\tsconfig.json", new TSConfigResponse
                {
                    Files = new List<string>{testedFileName}

                }}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("c:\\a\\b\\tsconfig.json");
        }

        private static void SetupEslintBridgeResponse(
            Mock<ITypeScriptEslintBridgeClient> eslintBridgeClient,
            IDictionary<string, TSConfigResponse> response)
        {
            foreach (var responseValues in response)
            {
                eslintBridgeClient
                    .Setup(x => x.TsConfigFiles(responseValues.Key, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(responseValues.Value);
            }
        }

        private Mock<ITsConfigsLocator> SetupTsConfigsLocator(string sourceFilePath, IReadOnlyList<string> tsConfigsInSolution)
        {
            var tsConfigsLocator = new Mock<ITsConfigsLocator>();
            tsConfigsLocator.Setup(x => x.Locate(sourceFilePath)).Returns(tsConfigsInSolution);

            return tsConfigsLocator;
        }

        private TsConfigProvider CreateTestSubject(
            ITsConfigsLocator tsConfigsLocator,
            ITypeScriptEslintBridgeClient eslintBridgeClient,
            ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            return new TsConfigProvider(tsConfigsLocator, eslintBridgeClient, logger);
        }
    }
}
