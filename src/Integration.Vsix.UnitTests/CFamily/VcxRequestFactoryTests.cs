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
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests.CFamilyTestUtility;
using VsShell = Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class VcxRequestFactoryTests
    {
        private static ProjectItem DummyProjectItem = Mock.Of<ProjectItem>();
        private static CFamilyAnalyzerOptions DummyAnalyzerOptions = new CFamilyAnalyzerOptions();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VcxRequestFactory, IRequestFactory>(
                MefTestHelpers.CreateExport<VsShell.SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ICFamilyRulesConfigProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<VcxRequestFactory>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var cFamilyRulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();
            var logger = new Mock<ILogger>();
            var threadHandling = new Mock<IThreadHandling>();

            _ = new VcxRequestFactory(serviceProvider.Object, cFamilyRulesConfigProvider.Object, logger.Object, threadHandling.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceProvider.Invocations.Should().BeEmpty();
            cFamilyRulesConfigProvider.Invocations.Should().BeEmpty();
            logger.Invocations.Should().BeEmpty();
            threadHandling.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task TryGet_RunsOnUIThread()
        {
            var fileConfigProvider = new Mock<IFileConfigProvider>();

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(op =>
                {
                    // Try to check that the product code is executed inside the "RunOnUIThread" call
                    fileConfigProvider.Invocations.Count.Should().Be(0);
                    op();
                    fileConfigProvider.Invocations.Count.Should().Be(1);
                });

            var testSubject = CreateTestSubject(projectItem: DummyProjectItem,
                threadHandling: threadHandling.Object,
                fileConfigProvider: fileConfigProvider.Object);


            var request = await testSubject.TryCreateAsync("any", new CFamilyAnalyzerOptions());

            threadHandling.Verify(x => x.RunOnUIThreadAsync(It.IsAny<Action>()), Times.Once);
            threadHandling.Verify(x => x.ThrowIfNotOnUIThread(), Times.Exactly(2));
        }

        [TestMethod]
        public async Task TryGet_NoProjectItem_Null()
        {
            var testSubject = CreateTestSubject(projectItem: null);

            var request = await testSubject.TryCreateAsync("path", new CFamilyAnalyzerOptions());

            request.Should().BeNull();
        }

        [TestMethod]
        public async Task TryGet_NoFileConfig_Null()
        {
            const string analyzedFilePath = "path";
            var fileConfigProvider = SetupFileConfigProvider(DummyProjectItem, DummyAnalyzerOptions, analyzedFilePath, null);

            var testSubject = CreateTestSubject(DummyProjectItem, fileConfigProvider: fileConfigProvider.Object);
            var request = await testSubject.TryCreateAsync("path", DummyAnalyzerOptions);

            request.Should().BeNull();

            fileConfigProvider.Verify(x => x.Get(DummyProjectItem, analyzedFilePath, DummyAnalyzerOptions), Times.Once);
        }

        [TestMethod]
        public async Task TryGet_RequestCreatedWithNoDetectedLanguage_Null()
        {
            const string analyzedFilePath = "c:\\notCFamilyFile.txt";

            var fileConfig = CreateDummyFileConfig(analyzedFilePath);
            var fileConfigProvider = SetupFileConfigProvider(DummyProjectItem, DummyAnalyzerOptions, analyzedFilePath, fileConfig.Object);
            var cFamilyRulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(DummyProjectItem,
                fileConfigProvider: fileConfigProvider.Object,
                cFamilyRulesConfigProvider: cFamilyRulesConfigProvider.Object);

            var request = await testSubject.TryCreateAsync(analyzedFilePath, DummyAnalyzerOptions);

            request.Should().BeNull();

            fileConfig.VerifyGet(x => x.CDFile, Times.Once);
            cFamilyRulesConfigProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task TryGet_FailureParsing_NonCriticialException_Null()
        {
            const string analyzedFilePath = "c:\\test.cpp";

            var fileConfig = CreateDummyFileConfig(analyzedFilePath);
            var fileConfigProvider = SetupFileConfigProvider(DummyProjectItem, DummyAnalyzerOptions, analyzedFilePath, fileConfig.Object);

            var cFamilyRulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();
            cFamilyRulesConfigProvider
                .Setup(x => x.GetRulesConfiguration(SonarLanguageKeys.CPlusPlus))
                .Throws<NotImplementedException>();

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(DummyProjectItem,
                fileConfigProvider: fileConfigProvider.Object,
                cFamilyRulesConfigProvider: cFamilyRulesConfigProvider.Object,
                logger: logger);

            var request = await testSubject.TryCreateAsync(analyzedFilePath, DummyAnalyzerOptions);

            request.Should().BeNull();

            logger.AssertPartialOutputStringExists(nameof(NotImplementedException));
        }

        [TestMethod]
        public void TryGet_FailureParsing_CriticalException_ExceptionThrown()
        {
            const string analyzedFilePath = "c:\\test.cpp";

            var fileConfig = CreateDummyFileConfig(analyzedFilePath);
            var fileConfigProvider = SetupFileConfigProvider(DummyProjectItem, DummyAnalyzerOptions, analyzedFilePath, fileConfig.Object);

            var cFamilyRulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();
            cFamilyRulesConfigProvider
                .Setup(x => x.GetRulesConfiguration(SonarLanguageKeys.CPlusPlus))
                .Throws<StackOverflowException>();

            var testSubject = CreateTestSubject(DummyProjectItem,
                fileConfigProvider: fileConfigProvider.Object,
                cFamilyRulesConfigProvider: cFamilyRulesConfigProvider.Object);

            Func<Task> act = () => testSubject.TryCreateAsync(analyzedFilePath, DummyAnalyzerOptions);

            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public async Task TryGet_IRequestPropertiesAreSet()
        {
            var analyzerOptions = new CFamilyAnalyzerOptions();
            var rulesConfig = Mock.Of<ICFamilyRulesConfig>();

            var request = await GetSuccessfulRequest(analyzerOptions, "d:\\xxx\\fileToAnalyze.cpp", rulesConfig);
            request.Should().NotBeNull();

            request.Context.File.Should().Be("d:\\xxx\\fileToAnalyze.cpp");
            request.Context.PchFile.Should().Be(SubProcessFilePaths.PchFilePath);
            request.Context.AnalyzerOptions.Should().BeSameAs(analyzerOptions);
            request.Context.RulesConfiguration.Should().BeSameAs(rulesConfig);
        }

        [TestMethod]
        public async Task TryGet_FileConfigIsSet()
        {
            var request = await GetSuccessfulRequest();
            request.Should().NotBeNull();

            request.DatabaseEntry.Should().NotBeNull();
        }

        [TestMethod]
        public async Task TryGet_NonHeaderFile_IsSupported()
        {
            var request = await GetSuccessfulRequest();

            request.Should().NotBeNull();
            request.Context.IsHeaderFile.Should().Be(false);
        }

        [TestMethod]
        public async Task TryGet_HeaderFile_IsSupported()
        {
            var projectItemConfig = new ProjectItemConfig { ItemType = "ClInclude" };
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            var fileConfig = CreateDummyFileConfig("c:\\dummy\\file.h");
            fileConfig.Setup(x => x.HeaderFileLanguage).Returns("cpp");

            var request = await GetSuccessfulRequest(fileToAnalyze: "c:\\dummy\\file.h", projectItem: projectItemMock.Object, fileConfig: fileConfig);

            request.Should().NotBeNull();
            request.Context.IsHeaderFile.Should().Be(true);
        }

        [TestMethod]
        public async Task TryGet_NoAnalyzerOptions_RequestCreatedWithoutOptions()
        {
            var request = await GetSuccessfulRequest(analyzerOptions: null);
            request.Should().NotBeNull();

            (request.Context.AnalyzerOptions).Should().BeNull();
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithReproducerEnabled_RequestCreatedWithReproducerFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreateReproducer = true });
            request.Should().NotBeNull();

            (request.Context.AnalyzerOptions.CreateReproducer).Should().Be(true);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithoutReproducerEnabled_RequestCreatedWithoutReproducerFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreateReproducer = false });
            request.Should().NotBeNull();

            (request.Context.AnalyzerOptions.CreateReproducer).Should().Be(false);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithPCH_RequestCreatedWithPCHFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = true });
            request.Should().NotBeNull();

            (request.Context.AnalyzerOptions.CreatePreCompiledHeaders).Should().Be(true);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithoutPCH_RequestCreatedWithoutPCHFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = false });
            request.Should().NotBeNull();

            (request.Context.AnalyzerOptions.CreatePreCompiledHeaders).Should().Be(false);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithPCH_RequestOptionsNotSet()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = true });
            request.Should().NotBeNull();

            request.Context.RulesConfiguration.Should().BeNull();
            (request.Context.AnalyzerOptions.CreateReproducer).Should().Be(false);
            (request.Context.AnalyzerOptions.CreatePreCompiledHeaders).Should().Be(true);
        }

        private static Mock<IFileConfigProvider> SetupFileConfigProvider(ProjectItem projectItem,
            CFamilyAnalyzerOptions analyzerOptions,
            string analyzedFilePath,
            IFileConfig fileConfigToReturn)
        {
            var fileConfigProvider = new Mock<IFileConfigProvider>();
            fileConfigProvider
                .Setup(x => x.Get(projectItem, analyzedFilePath, analyzerOptions))
                .Returns(fileConfigToReturn);

            return fileConfigProvider;
        }

        private VcxRequestFactory CreateTestSubject(ProjectItem projectItem,
            ICFamilyRulesConfigProvider cFamilyRulesConfigProvider = null,
            IFileConfigProvider fileConfigProvider = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            var serviceProvider = CreateServiceProviderReturningProjectItem(projectItem);

            cFamilyRulesConfigProvider ??= Mock.Of<ICFamilyRulesConfigProvider>();
            fileConfigProvider ??= Mock.Of<IFileConfigProvider>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= Mock.Of<ILogger>();

            return new VcxRequestFactory(serviceProvider.Object,
                cFamilyRulesConfigProvider,
                new Lazy<IFileConfigProvider>(() => fileConfigProvider),
                logger,
                threadHandling);
        }

        private static Mock<IServiceProvider> CreateServiceProviderReturningProjectItem(ProjectItem projectItemToReturn)
        {
            var mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>())).Returns(projectItemToReturn);

            var mockDTE = new Mock<DTE2>();
            mockDTE.Setup(d => d.Solution).Returns(mockSolution.Object);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(SDTE))).Returns(mockDTE.Object);

            return mockServiceProvider;
        }

        private async Task<CompilationDatabaseRequest> GetSuccessfulRequest(CFamilyAnalyzerOptions analyzerOptions = null,
            string fileToAnalyze = "c:\\foo\\file.cpp",
            ICFamilyRulesConfig rulesConfig = null,
            ProjectItem projectItem = null,
            Mock<IFileConfig> fileConfig = null,
            RuleConfigProtocolFormat protocolFormat = null)
        {
            rulesConfig ??= Mock.Of<ICFamilyRulesConfig>();

            var rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();

            rulesConfigProviderMock
                .Setup(x => x.GetRulesConfiguration(It.IsAny<string>()))
                .Returns(rulesConfig);

            projectItem ??= Mock.Of<ProjectItem>();

            fileConfig ??= CreateDummyFileConfig(fileToAnalyze);
            var fileConfigProvider = SetupFileConfigProvider(projectItem, analyzerOptions, fileToAnalyze, fileConfig.Object);

            protocolFormat ??= new RuleConfigProtocolFormat("qp", new Dictionary<string, string>());


            var testSubject = CreateTestSubject(projectItem,
                rulesConfigProviderMock.Object,
                fileConfigProvider.Object);

            return await testSubject.TryCreateAsync(fileToAnalyze, analyzerOptions) as CompilationDatabaseRequest;
        }

        private Mock<IFileConfig> CreateDummyFileConfig(string filePath)
        {
            var fileConfig = new Mock<IFileConfig>();

            fileConfig.SetupGet(x => x.CDDirectory).Returns("c:\\");
            fileConfig.SetupGet(x => x.CDCommand).Returns("cl.exe " + filePath);
            fileConfig.SetupGet(x => x.CDFile).Returns(filePath);
            fileConfig.SetupGet(x => x.EnvInclude).Returns("");
            fileConfig.SetupGet(x => x.HeaderFileLanguage).Returns("");

            return fileConfig;
        }
    }
}
