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

using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
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
            MefTestHelpers.CheckTypeCanBeImported<TsConfigProvider, ITsConfigProvider>(
                MefTestHelpers.CreateExport<ITsConfigsLocator>(),
                MefTestHelpers.CreateExport<ITypeScriptEslintBridgeClient>(),
                MefTestHelpers.CreateExport<ILogger>());
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
            var tsConfigsInSolution = new[] { "c:\\config1", "c:\\config2", "c:\\config3" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"c:\\config1", new TSConfigResponse()},
                {"c:\\config2", new TSConfigResponse{Files = new List<string>{"some other file"}}},
                {"c:\\config3", new TSConfigResponse{Files = new List<string>{"some file2"}}},
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config3", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("tested\\file")] // exact match
        [DataRow("tested\\FILE")] // case-insensitive
        [DataRow("tested/FILE")] // different slashes
        public async Task GetConfigForFile_SourceFileFoundInTsConfig_OtherTsConfigsNotChecked(string foundFileName)
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "c:\\config1", "c:\\config2", "c:\\config3" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"c:\\config1", new TSConfigResponse()},
                {"c:\\config2", new TSConfigResponse{Files = new List<string>{foundFileName}}},
                {"c:\\config3", new TSConfigResponse()},
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("c:\\config2");

            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config3", CancellationToken.None), Times.Never);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigContainsFileWithIllegalCharacters_FileInTsConfigIsSkipped()
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "c:\\config" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"c:\\config", new TSConfigResponse{Files = new List<string>
                {
                    "validPath",
                    "invalid\\*",
                    testedFileName
                }}}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("c:\\config");
        }

        [TestMethod]
        public async Task GetConfigForFile_FailedToProcessTsConfig_TsConfigIsSkipped()
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "c:\\config1", "c:\\config2" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {
                    "c:\\config1", new TSConfigResponse
                    {
                        Error = "some error",
                        Files = new[] {testedFileName} // should be ignored
                    }
                },
                {"c:\\config2", new TSConfigResponse()}
            });

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object, logger: logger);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();

            logger.AssertPartialOutputStringExists("some error");
        }

        [TestMethod]
        public async Task GetConfigForFile_FailedToProcessTsConfig_ParsingError_TsConfigIsSkipped()
        {
            const string testedFileName = "tested\\file";
            var tsConfigsInSolution = new[] { "c:\\config1", "c:\\config2" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();
            var parsingError = new ParsingError { Code = ParsingErrorCode.UNSUPPORTED_TYPESCRIPT, Message = "some message", Line = 5555 };
            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {
                    "c:\\config1", new TSConfigResponse
                    {
                        ParsingError = parsingError,
                        Files = new[] {testedFileName} // should be ignored
                    }
                },
                {"c:\\config2", new TSConfigResponse()}
            });

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object, logger: logger);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().BeNull();

            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();

            logger.AssertPartialOutputStringExists(parsingError.Message, parsingError.Code.ToString(), parsingError.Line.ToString());
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasProjectReferences_ProjectReferencesAreChecked()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "c:\\config1", "c:\\config2" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"c:\\config1", new TSConfigResponse()},
                {"c:\\config2", new TSConfigResponse
                {
                    Files = new List<string>{"some other file"},
                    ProjectReferences = new List<string>{ "c:\\config3" }

                }},
                {"c:\\config3", new TSConfigResponse{Files = new List<string>{testedFileName}}}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("c:\\config3");

            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config3", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasProjectReferences_ProjectReferencesAreCheckedBeforeFiles()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "c:\\config1" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"c:\\config1", new TSConfigResponse
                {
                    Files = new List<string>{testedFileName},
                    ProjectReferences = new List<string>{ "c:\\config2" }
                }},
                {"c:\\config2", new TSConfigResponse
                {
                    Files = new List<string>{testedFileName}
                }}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("c:\\config2");

            // Veify that project references are checked before "TSConfigResponse.Files"
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config2", CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasProjectReferences_CircularReferencesAreIgnored()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "c:\\config1", "c:\\config3" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"c:\\config1", new TSConfigResponse{ProjectReferences = new List<string>{"c:\\config2"}}},
                {"c:\\config2", new TSConfigResponse
                {
                    Files = new List<string>{"some other file"},
                    ProjectReferences = new List<string>{ "c:\\config1" } // circular loop

                }},
                {"c:\\config3", new TSConfigResponse{Files = new List<string>{testedFileName}}}
            });

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object);
            var result = await testSubject.GetConfigForFile(testedFileName, CancellationToken.None);
            result.Should().Be("c:\\config3");

            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config1", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config2", CancellationToken.None), Times.Once);
            eslintBridgeClient.Verify(x => x.TsConfigFiles("c:\\config3", CancellationToken.None), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task GetConfigForFile_TsConfigHasFolderProjectReference_ReferenceProcessedWithDefaultTsConfigName()
        {
            const string testedFileName = "some file";
            var tsConfigsInSolution = new[] { "c:\\config1" };
            var tsConfigsLocator = SetupTsConfigsLocator(testedFileName, tsConfigsInSolution);

            var eslintBridgeClient = new Mock<ITypeScriptEslintBridgeClient>();

            SetupEslintBridgeResponse(eslintBridgeClient, new Dictionary<string, TSConfigResponse>
            {
                {"c:\\config1", new TSConfigResponse
                {
                    ProjectReferences = new List<string>{"c:/a/b"}
                }},
                {"c:\\a\\b\\tsconfig.json", new TSConfigResponse
                {
                    Files = new List<string>{testedFileName}

                }}
            });

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists("c:\\config1")).Returns(false);
            fileSystem.Setup(x => x.Directory.Exists("c:/a/b")).Returns(true);

            var testSubject = CreateTestSubject(tsConfigsLocator.Object, eslintBridgeClient.Object, fileSystem.Object);
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
            IFileSystem fileSystem = null,
            ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            if (fileSystem == null)
            {
                var fileSystemMock = new Mock<IFileSystem>();
                fileSystemMock.Setup(x => x.Directory.Exists(It.IsAny<string>())).Returns(false);
                fileSystem = fileSystemMock.Object;
            }

            return new TsConfigProvider(tsConfigsLocator, eslintBridgeClient, fileSystem, logger);
        }
    }
}
