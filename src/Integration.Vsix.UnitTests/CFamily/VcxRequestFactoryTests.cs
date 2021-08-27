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
using EnvDTE;
using FluentAssertions;
using VsShell = Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests.CFamilyTestUtility;
using System.Threading.Tasks;

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
            MefTestHelpers.CheckTypeCanBeImported<VcxRequestFactory, IRequestFactory>(null, new[]
            {
                MefTestHelpers.CreateExport<VsShell.SVsServiceProvider>(Mock.Of<IServiceProvider>()),
                MefTestHelpers.CreateExport<ICFamilyRulesConfigProvider>(Mock.Of<ICFamilyRulesConfigProvider>()),
                MefTestHelpers.CreateExport<IThreadHandling>(Mock.Of<IThreadHandling>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public async Task TryGet_RunsOnUIThread()
        {
            var threadHandling = CreateRunnableThreadHandling();
            var testSubject = CreateTestSubject(projectItem: null,
                threadHandling: threadHandling.Object);

            var request = await testSubject.TryCreateAsync("any", new CFamilyAnalyzerOptions());

            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()));
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

            fileConfigProvider.Verify(x=> x.Get(DummyProjectItem, analyzedFilePath, DummyAnalyzerOptions), Times.Once);
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

            fileConfig.VerifyGet(x => x.AbsoluteFilePath, Times.Once);
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
        public async Task TryGet_FailureParsing_CriticalException_ExceptionThrown()
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

            request.FileConfig.Should().NotBeNull();
        }

        [TestMethod]
        public async Task TryGet_NonHeaderFile_IsSupported()
        {
            var request = await GetSuccessfulRequest();
            
            request.Should().NotBeNull();
            (request.Flags & Request.MainFileIsHeader).Should().Be(0);
        }

        [TestMethod]
        public async Task TryGet_HeaderFile_IsSupported()
        {
            var projectItemConfig = new ProjectItemConfig { ItemType = "ClInclude" };
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            var fileConfig = CreateDummyFileConfig("c:\\dummy\\file.h");
            fileConfig.Setup(x => x.CompileAs).Returns("CompileAsCpp");
            fileConfig.Setup(x => x.ItemType).Returns("ClInclude");

            var request = await GetSuccessfulRequest(fileToAnalyze: "c:\\dummy\\file.h", projectItem: projectItemMock.Object, fileConfig: fileConfig);

            request.Should().NotBeNull();
            (request.Flags & Request.MainFileIsHeader).Should().NotBe(0);
        }

        [TestMethod]
        public async Task TryGet_NoAnalyzerOptions_RequestCreatedWithoutOptions()
        {
            var request = await GetSuccessfulRequest(analyzerOptions: null);
            request.Should().NotBeNull();

            (request.Flags & Request.CreateReproducer).Should().Be(0);
            (request.Flags & Request.BuildPreamble).Should().Be(0);
            
            request.Context.AnalyzerOptions.Should().BeNull();
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithReproducerEnabled_RequestCreatedWithReproducerFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreateReproducer = true });
            request.Should().NotBeNull();

            (request.Flags & Request.CreateReproducer).Should().NotBe(0);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithoutReproducerEnabled_RequestCreatedWithoutReproducerFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreateReproducer = false });
            request.Should().NotBeNull();

            (request.Flags & Request.CreateReproducer).Should().Be(0);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithPCH_RequestCreatedWithPCHFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = true });
            request.Should().NotBeNull();

            (request.Flags & Request.BuildPreamble).Should().NotBe(0);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithoutPCH_RequestCreatedWithoutPCHFlag()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = false });
            request.Should().NotBeNull();

            (request.Flags & Request.BuildPreamble).Should().Be(0);
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithPCH_RequestOptionsNotSet()
        {
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = true });
            request.Should().NotBeNull();

            request.Context.RulesConfiguration.Should().BeNull();
            request.Options.Should().BeEmpty();
        }

        [TestMethod]
        public async Task TryGet_AnalyzerOptionsWithoutPCH_RequestOptionsAreSet()
        {
            var rulesProtocolFormat = new RuleConfigProtocolFormat("some profile", new Dictionary<string, string>
            {
                {"rule1", "param1"},
                {"rule2", "param2"}
            });
            
            var request = await GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = false }, protocolFormat: rulesProtocolFormat);
            request.Should().NotBeNull();

            request.Context.RulesConfiguration.Should().NotBeNull();
            request.Options.Should().BeEquivalentTo(
                "internal.qualityProfile=some profile",
                "rule1=param1",
                "rule2=param2");
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
            IRulesConfigProtocolFormatter rulesConfigProtocolFormatter = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            var serviceProvider = CreateServiceProviderReturningProjectItem(projectItem);

            cFamilyRulesConfigProvider ??= Mock.Of<ICFamilyRulesConfigProvider>();
            rulesConfigProtocolFormatter ??= Mock.Of<IRulesConfigProtocolFormatter>();
            fileConfigProvider ??= Mock.Of<IFileConfigProvider>();
            threadHandling ??= CreateRunnableThreadHandling().Object;
            logger ??= Mock.Of<ILogger>();

            return new VcxRequestFactory(serviceProvider.Object, 
                cFamilyRulesConfigProvider,
                threadHandling,
                rulesConfigProtocolFormatter,
                fileConfigProvider,
                logger);
        }

        private static Mock<IServiceProvider> CreateServiceProviderReturningProjectItem(ProjectItem projectItemToReturn)
        {
            var mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>())).Returns(projectItemToReturn);

            var mockDTE = new Mock<DTE>();
            mockDTE.Setup(d => d.Solution).Returns(mockSolution.Object);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(DTE))).Returns(mockDTE.Object);

            return mockServiceProvider;
        }

        private async Task<Request> GetSuccessfulRequest(CFamilyAnalyzerOptions analyzerOptions = null,
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

            var rulesConfigProtocolFormatter = new Mock<IRulesConfigProtocolFormatter>();
            rulesConfigProtocolFormatter
                .Setup(x => x.Format(rulesConfig))
                .Returns(protocolFormat);

            var testSubject = CreateTestSubject(projectItem, 
                rulesConfigProviderMock.Object, 
                fileConfigProvider.Object,
                rulesConfigProtocolFormatter.Object);

            return await testSubject.TryCreateAsync(fileToAnalyze, analyzerOptions) as Request;
        }

        private static Mock<IThreadHandling> CreateRunnableThreadHandling()
        {
            // Create a thread handling that will execute RunOnUIThread
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(op => op());
            return threadHandling;
        }

        private Mock<IFileConfig> CreateDummyFileConfig(string filePath)
        {
            var fileConfig = new Mock<IFileConfig>();

            fileConfig.SetupGet(x => x.PlatformName).Returns("Win32");
            fileConfig.SetupGet(x => x.AdditionalIncludeDirectories).Returns("");
            fileConfig.SetupGet(x => x.ForcedIncludeFiles).Returns("");
            fileConfig.SetupGet(x => x.PrecompiledHeader).Returns("");
            fileConfig.SetupGet(x => x.UndefinePreprocessorDefinitions).Returns("");
            fileConfig.SetupGet(x => x.PreprocessorDefinitions).Returns("");
            fileConfig.SetupGet(x => x.CompileAs).Returns("");
            fileConfig.SetupGet(x => x.CompileAsManaged).Returns("");
            fileConfig.SetupGet(x => x.RuntimeLibrary).Returns("");
            fileConfig.SetupGet(x => x.ExceptionHandling).Returns("");
            fileConfig.SetupGet(x => x.EnableEnhancedInstructionSet).Returns("");
            fileConfig.SetupGet(x => x.BasicRuntimeChecks).Returns("");
            fileConfig.SetupGet(x => x.LanguageStandard).Returns("");
            fileConfig.SetupGet(x => x.AdditionalOptions).Returns("");
            fileConfig.SetupGet(x => x.CompilerVersion).Returns("1.2.3.4");

            fileConfig.SetupGet(x => x.AbsoluteProjectPath).Returns("c:\\test.vcxproj");
            fileConfig.SetupGet(x => x.AbsoluteFilePath).Returns(filePath);

            return fileConfig;
        }
    }
}
