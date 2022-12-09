﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BindingProcessImplTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetsInformationProvider;
        private ConfigurableHost host;
        private Mock<IFolderWorkspaceService> folderWorkspaceService;
        private Mock<IJsTsProjectTypeIndicator> jstsIndicator;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            logger = new TestLogger();
            serviceProvider.RegisterService(typeof(ILogger), logger);

            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);

            var mockFileSystem = new MockFileSystem();
            var sccFileSystem = new ConfigurableSourceControlledFileSystem(mockFileSystem);
            var ruleSerializer = new ConfigurableRuleSetSerializer(mockFileSystem);
            this.ruleSetsInformationProvider = new ConfigurableSolutionRuleSetsInformationProvider();

            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), sccFileSystem);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), ruleSerializer);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetsInformationProvider);

            jstsIndicator = new Mock<IJsTsProjectTypeIndicator>();
            var projectToLanguageMapper = new ProjectToLanguageMapper(Mock.Of<ICMakeProjectTypeIndicator>(), jstsIndicator.Object);

            folderWorkspaceService = new Mock<IFolderWorkspaceService>();

            var mefProjectToLanguageMapper = 
                ConfigurableComponentModel.CreateWithExports(
                    MefTestHelpers.CreateExport<IProjectToLanguageMapper>(projectToLanguageMapper),
                    MefTestHelpers.CreateExport<IFolderWorkspaceService>(folderWorkspaceService.Object));

            serviceProvider.RegisterService(typeof(SComponentModel), mefProjectToLanguageMapper);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            host.Logger = logger;
        }

        #region Tests

        [TestMethod]
        public void Ctor_ArgChecks()
        {
            var validHost = new ConfigurableHost();
            var bindingArgs = CreateBindCommandArgs(connection: new ConnectionInformation(new Uri("http://server")));
            var slnBindOp = new Mock<ISolutionBindingOperation>().Object;
            var nuGetOp = new Mock<INuGetBindingOperation>().Object;
            var finder = new ConfigurableUnboundProjectFinder();
            var bindingConfigProvider = new Mock<IBindingConfigProvider>().Object;
            var exclusionSettingsStorage = Mock.Of<IExclusionSettingsStorage>();

            // 1. Null host
            Action act = () => new BindingProcessImpl(null, bindingArgs, slnBindOp, nuGetOp, finder, bindingConfigProvider, SonarLintMode.Connected, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");

            // 2. Null binding args
            act = () => new BindingProcessImpl(validHost, null, slnBindOp, nuGetOp, finder, bindingConfigProvider, SonarLintMode.Connected, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingArgs");

            // 3. Null solution binding operation
            act = () => new BindingProcessImpl(validHost, bindingArgs, null, nuGetOp, finder, bindingConfigProvider, SonarLintMode.Connected, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingOperation");

            // 4. Null NuGet operation
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, null, finder, bindingConfigProvider, SonarLintMode.Connected, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("nugetBindingOperation");

            // 5. Null binding info provider
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, nuGetOp, null, bindingConfigProvider, SonarLintMode.Connected, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("unboundProjectFinder");

            // 6. Null rules configuration provider
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, nuGetOp, finder, null, SonarLintMode.Connected, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingConfigProvider");

            // 6. Null exclusion settings storage
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, nuGetOp, finder, bindingConfigProvider, SonarLintMode.Connected, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exclusionSettingsStorage");
        }

        [TestMethod]
        public async Task SaveServerExclusionsAsync_ConnectedMode_ReturnsTrue()
        {
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            CreateConfigurationProviderService("C:\\SolutionPath");

            ServerExclusions settings = CreateSettings();

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Returns(Task.FromResult(settings));

            var exclusionSettingsStorage = new Mock<IExclusionSettingsStorage>();

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs, mode: SonarLintMode.Connected, sonarQubeService: sonarQubeService.Object, exclusionSettingsStorage: exclusionSettingsStorage.Object);
            
            await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            exclusionSettingsStorage.Verify(fs => fs.SaveSettings(settings), Times.Once);
            logger.AssertOutputStrings(0);
        }

        [TestMethod]
        public async Task SaveServerExclusionsAsync_HasError_ReturnsFalse()
        {
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            CreateConfigurationProviderService("C:\\SolutionPath");

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Throws(new Exception("Expected Error"));

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs, mode: SonarLintMode.Connected, sonarQubeService: sonarQubeService.Object);

            var result = await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            result.Should().BeFalse();
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStrings("Expected Error");
        }

        [TestMethod]
        public void SaveServerExclusionsAsync_HasCriticalError_Throws()
        {
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            CreateConfigurationProviderService("C:\\SolutionPath");

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Throws(new StackOverflowException("Critical Error"));

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs, mode: SonarLintMode.Connected, sonarQubeService: sonarQubeService.Object);

            Func<Task<bool>> act = async () =>  await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            act.Should().ThrowExactly<StackOverflowException>().WithMessage("Critical Error");
            logger.AssertOutputStrings(0);
            
        }

        private static ServerExclusions CreateSettings()
        {
            return new ServerExclusions
            {
                Inclusions = new string[] { "inclusion1", "inclusion2" },
                Exclusions = new string[] { "exclusion" },
                GlobalExclusions = new string[] { "globalExclusion" }
            };
        }

        private void CreateConfigurationProviderService(string folderPathToReturn)
        {
            var configurationProviderService = new ConfigurableConfigurationProvider();
            configurationProviderService.ModeToReturn = SonarLintMode.Connected;
            configurationProviderService.FolderPathToReturn = folderPathToReturn;

            this.serviceProvider.RegisterService(typeof(IConfigurationProviderService), configurationProviderService);
        }

        [TestMethod]
        public async Task DownloadQualityProfile_Success()
        {
            var configPersister = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationPersister), configPersister);

            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";

            Mock<INuGetBindingOperation> nuGetOpMock = new Mock<INuGetBindingOperation>();

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(notifications);

            var bindingConfig = new Mock<IBindingConfig>().Object;

            var language = Language.VBNET;
            SonarQubeQualityProfile profile = this.ConfigureQualityProfile(language, QualityProfileName);

            var configProviderMock = new Mock<IBindingConfigProvider>();
            configProviderMock.Setup(x => x.GetConfigurationAsync(profile, language, BindingConfiguration.Standalone, CancellationToken.None))
                .ReturnsAsync(bindingConfig);

            var bindingArgs = CreateBindCommandArgs("key", ProjectName, new ConnectionInformation(new Uri("http://connected")));
            var testSubject = this.CreateTestSubject(bindingArgs, nuGetOpMock.Object, configProviderMock.Object);

            ConfigureSupportedBindingProject(testSubject.InternalState, language);

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            testSubject.InternalState.BindingConfigs.Should().ContainKey(language);
            testSubject.InternalState.BindingConfigs[language].Should().Be(bindingConfig);
            testSubject.InternalState.BindingConfigs.Count().Should().Be(1);

            testSubject.InternalState.QualityProfiles[language].Should().Be(profile);

            notifications.AssertProgress(0.0, 1.0);
            notifications.AssertProgressMessages(Strings.DownloadingQualityProfileProgressMessage, string.Empty);

            logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.QualityProfileDownloadSuccessfulMessageFormat, QualityProfileName, string.Empty, language.Name));
            logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public async Task DownloadQualityProfile_SavesConfiguration()
        {
            var configPersister = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationPersister), configPersister);

            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";

            Mock<INuGetBindingOperation> nuGetOpMock = new Mock<INuGetBindingOperation>();

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(notifications);

            var bindingConfig = new Mock<IBindingConfig>().Object;

            var language = Language.VBNET;
            SonarQubeQualityProfile profile = this.ConfigureQualityProfile(language, QualityProfileName);

            var configProviderMock = new Mock<IBindingConfigProvider>();
            configProviderMock.Setup(x => x.GetConfigurationAsync(profile, language, It.IsAny<BindingConfiguration>(), CancellationToken.None))
                .ReturnsAsync(bindingConfig);

            var bindingArgs = CreateBindCommandArgs("key", ProjectName, new ConnectionInformation(new Uri("http://connected")));
            var testSubject = this.CreateTestSubject(bindingArgs, nuGetOpMock.Object, configProviderMock.Object);

            ConfigureSupportedBindingProject(testSubject.InternalState, language);

            // Act
            await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

            // Assert
            configPersister.SavedProject.Should().NotBeNull();
            configPersister.SavedMode.Should().Be(SonarLintMode.Connected);

            var savedProject = configPersister.SavedProject;
            savedProject.ServerUri.Should().Be(bindingArgs.Connection.ServerUri);
            savedProject.Profiles.Should().HaveCount(1);
            savedProject.Profiles[Language.VBNET].ProfileKey.Should().Be(profile.Key);
            savedProject.Profiles[Language.VBNET].ProfileTimestamp.Should().Be(profile.TimeStamp);
        }

        [TestMethod]
        public async Task DownloadQualityProfile_WhenQualityProfileIsNotAvailable_Fails()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(notifications);

            var language = Language.CSharp;
            ConfigureSupportedBindingProject(testSubject.InternalState, language);
            this.ConfigureQualityProfile(Language.VBNET, "");

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            testSubject.InternalState.BindingConfigs.Should().NotContainKey(Language.VBNET, "Not expecting any rules for this language");
            testSubject.InternalState.BindingConfigs.Should().NotContainKey(language, "Not expecting any rules");

            notifications.AssertProgressMessages(Strings.DownloadingQualityProfileProgressMessage);

            logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name));
            logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public async Task DownloadQualityProfile_WhenBindingConfigIsNull_Fails()
        {
            var configPersister = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationPersister), configPersister);

            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";

            Mock<INuGetBindingOperation> nuGetOpMock = new Mock<INuGetBindingOperation>();

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(notifications);

            var language = Language.VBNET;
            SonarQubeQualityProfile profile = this.ConfigureQualityProfile(language, QualityProfileName);

            var configProviderMock = new Mock<IBindingConfigProvider>();
            var bindingArgs = CreateBindCommandArgs("key", ProjectName, new ConnectionInformation(new Uri("http://connected")));
            var testSubject = this.CreateTestSubject(bindingArgs, nuGetOpMock.Object, configProviderMock.Object);

            ConfigureSupportedBindingProject(testSubject.InternalState, language);

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            testSubject.InternalState.QualityProfiles[language].Should().Be(profile);

            notifications.AssertProgress(0.0);
            notifications.AssertProgressMessages(Strings.DownloadingQualityProfileProgressMessage);

            logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.FailedToCreateBindingConfigForLanguage, language.Name));
            logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public void GetBindingLanguages_ReturnsDistinctLanguagesForProjects()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            var csProject1 = new ProjectMock("cs1.csproj");
            var csProject2 = new ProjectMock("cs2.csproj");
            var csProject3 = new ProjectMock("cs3.csproj");
            csProject1.SetCSProjectKind();
            csProject2.SetCSProjectKind();
            csProject3.SetCSProjectKind();
            var vbNetProject1 = new ProjectMock("vb1.vbproj");
            var vbNetProject2 = new ProjectMock("vb2.vbproj");
            vbNetProject1.SetVBProjectKind();
            vbNetProject2.SetVBProjectKind();
            var projects = new[]
            {
                csProject1,
                csProject2,
                vbNetProject1,
                csProject3,
                vbNetProject2
            };
            testSubject.InternalState.BindingProjects.AddRange(projects);

            var expectedLanguages = new[] { Language.CSharp, Language.VBNET };
            this.host.SupportedPluginLanguages.UnionWith(expectedLanguages);

            // Act
            var actualLanguages = testSubject.GetBindingLanguages();

            // Assert
            CollectionAssert.AreEquivalent(expectedLanguages, actualLanguages.ToArray(), "Unexpected languages for binding projects");
        }

        [TestMethod]
        public void GetBindingLanguages_IfCppIsDetected_ThenCIsReturnedToo()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            var project1 = new ProjectMock("cs1.csproj");
            project1.ProjectKind = ProjectSystemHelper.CppProjectKind;
            var projects = new[]
            {
                project1,
            };
            testSubject.InternalState.BindingProjects.AddRange(projects);

            this.host.SupportedPluginLanguages.Add(Language.Cpp);
            this.host.SupportedPluginLanguages.Add(Language.C);

            // Act
            var actualLanguages = testSubject.GetBindingLanguages();

            // Assert
            var expectedLanguages = new[] { Language.Cpp, Language.C };
            CollectionAssert.AreEquivalent(expectedLanguages, actualLanguages.ToArray());
        }

        [TestMethod]
        public void GetBindingLanguages_FiltersProjectsWithUnsupportedPluginLanguage()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            var csProject1 = new ProjectMock("cs1.csproj");
            var csProject2 = new ProjectMock("cs2.csproj");
            var csProject3 = new ProjectMock("cs3.csproj");
            csProject1.SetCSProjectKind();
            csProject2.SetCSProjectKind();
            csProject3.SetCSProjectKind();
            var vbNetProject1 = new ProjectMock("vb1.vbproj");
            var vbNetProject2 = new ProjectMock("vb2.vbproj");
            vbNetProject1.SetVBProjectKind();
            vbNetProject2.SetVBProjectKind();
            var projects = new[]
            {
                csProject1,
                csProject2,
                vbNetProject1,
                csProject3,
                vbNetProject2
            };
            testSubject.InternalState.BindingProjects.AddRange(projects);

            var expectedLanguages = new[] { Language.VBNET };
            this.host.SupportedPluginLanguages.UnionWith(expectedLanguages);

            // Act
            var actualLanguages = testSubject.GetBindingLanguages();

            // Assert
            CollectionAssert.AreEquivalent(expectedLanguages, actualLanguages.ToArray(), "Unexpected languages for binding projects");
        }

        [TestMethod]
        public void InstallPackages_Succeeds_SuccessPropertyIsTrue()
        {
            // Arrange
            var bindingArgs = CreateBindCommandArgs("projectKey", "projectName", new ConnectionInformation(new Uri("http://connected")));

            var slnBindOpMock = new Mock<ISolutionBindingOperation>();
            var nugetMock = new Mock<INuGetBindingOperation>();
            nugetMock.Setup(x => x.InstallPackages(It.IsAny<ISet<Project>>(),
                It.IsAny<IProgress<FixedStepsProgress>>(),
                It.IsAny<CancellationToken>())).Returns(true);
            var finder = new ConfigurableUnboundProjectFinder();
            var configProvider = new Mock<IBindingConfigProvider>();
            var exclusionSettingsStorage = Mock.Of<IExclusionSettingsStorage>();

            var testSubject = new BindingProcessImpl(this.host, bindingArgs, slnBindOpMock.Object, nugetMock.Object, finder, configProvider.Object, SonarLintMode.Connected, exclusionSettingsStorage);

            ProjectMock project1 = new ProjectMock("project1") { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            testSubject.InternalState.BindingProjects.Clear();
            testSubject.InternalState.BindingProjects.Add(project1);

            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(progressEvents);
            var cts = new CancellationTokenSource();

            testSubject.InternalState.BindingOperationSucceeded = false;

            // Act
            testSubject.InstallPackages(progressAdapter, cts.Token);

            // Assert
            testSubject.InternalState.BindingOperationSucceeded.Should().BeTrue();
        }

        [TestMethod]
        public void InstallPackages_Succeeds_SuccessPropertyIsFalse()
        {
            // Arrange
            var bindingArgs = CreateBindCommandArgs("projectKey", "projectName", new ConnectionInformation(new Uri("http://connected")));

            var slnBindOpMock = new Mock<ISolutionBindingOperation>();
            var nugetMock = new Mock<INuGetBindingOperation>();
            nugetMock.Setup(x => x.InstallPackages(It.IsAny<ISet<Project>>(),
                It.IsAny<IProgress<FixedStepsProgress>>(),
                It.IsAny<CancellationToken>())).Returns(false);
            var finder = new ConfigurableUnboundProjectFinder();
            var configProvider = new Mock<IBindingConfigProvider>();
            var exclusionSettingsStorage = Mock.Of<IExclusionSettingsStorage>();

            var testSubject = new BindingProcessImpl(this.host, bindingArgs, slnBindOpMock.Object, nugetMock.Object, finder, configProvider.Object, SonarLintMode.Connected, exclusionSettingsStorage);

            ProjectMock project1 = new ProjectMock("project1") { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            testSubject.InternalState.BindingProjects.Clear();
            testSubject.InternalState.BindingProjects.Add(project1);

            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(progressEvents);
            var cts = new CancellationTokenSource();

            testSubject.InternalState.BindingOperationSucceeded = true;

            // Act
            testSubject.InstallPackages(progressAdapter, cts.Token);

            // Assert
            testSubject.InternalState.BindingOperationSucceeded.Should().BeFalse();
        }

        [TestMethod]
        public void EmitBindingCompleteMessage()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            // Test case 1: Default state is 'true'
            testSubject.InternalState.BindingOperationSucceeded.Should().BeTrue($"Initial state of {nameof(BindingProcessImpl.InternalState.BindingOperationSucceeded)} should be true");
        }

        [TestMethod]
        public void PromptSaveSolutionIfDirty()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);

            // Case 1: Users saves the changes
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) => VSConstants.S_OK;
            // Act
            var result = testSubject.PromptSaveSolutionIfDirty();
            // Assert
            result.Should().BeTrue();
            logger.AssertOutputStrings(0);

            // Case 2: Users cancels the save
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) => VSConstants.S_FALSE;
            // Act
            result = testSubject.PromptSaveSolutionIfDirty();
            // Assert
            result.Should().BeFalse();
            logger.AssertOutputStrings(Strings.SolutionSaveCancelledBindAborted);
        }

        [TestMethod]
        public void SilentSaveSolutionIfDirty()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) => VSConstants.S_OK;

            // Act
            testSubject.SilentSaveSolutionIfDirty();

            // Assert
            logger.AssertOutputStrings(0);
        }

        [TestMethod]
        public void DiscoverBindableProjects_AddsMatchingProjectsToBinding()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();

            var csProject1 = new ProjectMock("cs1.csproj");
            var csProject2 = new ProjectMock("cs2.csproj");
            csProject1.SetCSProjectKind();
            csProject2.SetCSProjectKind();

            var matchingProjects = new[] { csProject1, csProject2 };
            this.projectSystemHelper.FilteredProjects = matchingProjects;

            var testSubject = this.CreateTestSubject();
            this.host.SupportedPluginLanguages.UnionWith(new[] { Language.CSharp });

            // Act
            var result = testSubject.DiscoverBindableProjects();

            // Assert
            result.Should().BeTrue();
            CollectionAssert.AreEqual(matchingProjects, testSubject.InternalState.BindingProjects.ToArray(), "Unexpected projects selected for binding");
        }

        private void DiscoverBindableProjects_GenericPart(int numberOfProjectsToCreate, int numberOfProjectsToInclude, bool expectedResult)
        {
            // Arrange
            List<Project> projects = new List<Project>();
            for (int i = 0; i < numberOfProjectsToCreate; i++)
            {
                var project = new ProjectMock($"cs{i}.csproj");
                project.SetCSProjectKind();
                projects.Add(project);
            }

            this.projectSystemHelper.FilteredProjects = projects.Take(numberOfProjectsToInclude);
            this.projectSystemHelper.Projects = projects;

            var testSubject = this.CreateTestSubject();
            this.host.SupportedPluginLanguages.UnionWith(new[] { Language.CSharp });

            // Act
            var result = testSubject.DiscoverBindableProjects();

            // Assert
            result.Should().Be(expectedResult);
            testSubject.InternalState.BindingProjects.Should().HaveCount(numberOfProjectsToInclude, "Expected " + numberOfProjectsToInclude + " project(s) selected for binding");
            logger.AssertOutputStrings(1);

            // Returns expected output message
            var expectedOutput = new StringBuilder();
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionIncludedProjectsHeader).AppendLine();
            if (numberOfProjectsToInclude > 0)
            {
                this.projectSystemHelper.FilteredProjects.ToList().ForEach(p => expectedOutput.AppendFormat("   * {0}\r\n", p.Name));
            }
            else
            {
                var msg = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, Strings.NoProjectsExcludedFromBinding);
                expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, msg).AppendLine();
            }
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionExcludedProjectsHeader).AppendLine();
            if (numberOfProjectsToCreate - numberOfProjectsToInclude > 0)
            {
                this.projectSystemHelper.Projects.Except(this.projectSystemHelper.FilteredProjects)
                                                 .ToList()
                                                 .ForEach(p => expectedOutput.AppendFormat("   * {0}\r\n", p.Name));
            }
            else
            {
                var msg = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, Strings.NoProjectsExcludedFromBinding);
                expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, msg).AppendLine();
            }
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, Strings.FilteredOutProjectFromBindingEnding);

            logger.AssertOutputStrings(expectedOutput.ToString());
        }

        [TestMethod]
        public void DiscoverBindableProjects_OutputsIncludedProjects()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();

            // Act & Common Assert
            DiscoverBindableProjects_GenericPart(2, 2, true);
        }

        [TestMethod]
        public void DiscoverBindableProjects_OutputsExcludedProjects()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();

            // Act & Common Assert
            DiscoverBindableProjects_GenericPart(2, 0, false);
        }

        [TestMethod]
        public void DiscoverBindableProjects_OutputsIncludedAndExcludedProjects()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();

            // Act & Common Assert
            DiscoverBindableProjects_GenericPart(4, 2, true);
        }

        [TestMethod]
        public void DiscoverBindableProjects_NoMatchingProjects_AbortsWorkflow()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();

            // Act & Common Assert
            DiscoverBindableProjects_GenericPart(0, 0, false);
        }

        [TestMethod]
        public void DiscoverBindableProjects_FolderWorkspace_HasSupportedLanguages_True()
        {
            folderWorkspaceService.Setup(x => x.IsFolderWorkspace()).Returns(true);
            jstsIndicator.Setup(x => x.IsJsTs(It.IsAny<Project>())).Returns(true);

            var testSubject = this.CreateTestSubject();

            // Act
            var result = testSubject.DiscoverBindableProjects();

            // Assert
            result.Should().Be(true);
            testSubject.InternalState.BindingProjects.Should().BeEmpty();
            logger.AssertOutputStrings(0);
        }

        [TestMethod]
        public void DiscoverBindableProjects_FolderWorkspace_NoSupportedLanguages_False()
        {
            folderWorkspaceService.Setup(x => x.IsFolderWorkspace()).Returns(true);
            jstsIndicator.Setup(x => x.IsJsTs(It.IsAny<Project>())).Returns(false);

            var testSubject = this.CreateTestSubject();

            // Act
            var result = testSubject.DiscoverBindableProjects();

            // Assert
            result.Should().Be(false);
            testSubject.InternalState.BindingProjects.Should().BeEmpty();
            logger.AssertOutputStrings(1);

            // Returns expected output message
            var expectedOutput = new StringBuilder();
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionIncludedProjectsHeader)
                .AppendLine();

            var msg = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat,
                Strings.NoProjectsExcludedFromBinding);
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, msg).AppendLine();
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionExcludedProjectsHeader)
                .AppendLine();

            msg = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat,
                Strings.NoProjectsExcludedFromBinding);
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, msg).AppendLine();
            expectedOutput.AppendFormat(Strings.SubTextPaddingFormat, Strings.FilteredOutProjectFromBindingEnding);

            logger.AssertOutputStrings(expectedOutput.ToString());
        }

        [TestMethod]
        public void GetProjectsForRulesetBinding_FirstBinding_AllProjectsBound()
        {
            // Arrange
            var allProjects = new Project[]
            {
                new ProjectMock("cs1.csproj"),
                new ProjectMock("cs2.csproj"),
                new ProjectMock("cs3.csproj")
            };

            var logger = new TestLogger();
            var finder = new ConfigurableUnboundProjectFinder
            {
                UnboundProjects = allProjects
            };

            // Act
            var result = BindingProcessImpl.GetProjectsForRulesetBinding(true, allProjects, finder, logger, new NoOpThreadHandler());

            // Assert
            result.Should().BeEquivalentTo(allProjects);
            logger.AssertOutputStringExists(Strings.Bind_Ruleset_InitialBinding);
        }

        [TestMethod]
        public void GetProjectsForRulesetBinding_NoProjectsUpToDate()
        {
            // Arrange
            var allProjects = new Project[]
            {
                new ProjectMock("cs1.csproj"),
                new ProjectMock("cs2.csproj"),
                new ProjectMock("cs3.csproj")
            };

            var logger = new TestLogger();
            var finder = new ConfigurableUnboundProjectFinder
            {
                UnboundProjects = allProjects
            };

            // Act
            var result = BindingProcessImpl.GetProjectsForRulesetBinding(false, allProjects, finder, logger, new NoOpThreadHandler());

            // Assert
            result.Should().BeEquivalentTo(allProjects);
            logger.AssertOutputStringExists(Strings.Bind_Ruleset_AllProjectsNeedToBeUpdated);
        }

        [TestMethod]
        public void InitializeSolutionBinding_Update_NotAllUpToDate_SomeProjectsUpdated()
        {
            // Arrange
            var allProjects = new Project[]
            {
                new ProjectMock("csA.csproj"),
                new ProjectMock("csB.csproj"),
                new ProjectMock("csC.csproj"),
                new ProjectMock("csD.csproj")
            };

            var unboundProjects = new Project[]
            {
                new ProjectMock("XXX.csproj"),
                allProjects[1],
                allProjects[3]
            };

            var logger = new TestLogger();
            var finder = new ConfigurableUnboundProjectFinder
            {
                UnboundProjects = unboundProjects
            };

            // Act
            var result = BindingProcessImpl.GetProjectsForRulesetBinding(false, allProjects, finder, logger, new NoOpThreadHandler());

            // Assert
            result.Should().BeEquivalentTo(allProjects[1], allProjects[3]);
            logger.AssertOutputStringExists(Strings.Bind_Ruleset_SomeProjectsDoNotNeedToBeUpdated);
            logger.AssertPartialOutputStringExists("csA.csproj", "csC.csproj");
        }

        [TestMethod]
        public void InitializeSolutionBinding_Update_AllUpToDate_NoProjectsUpdated()
        {
            // Arrange
            var allProjects = new Project[]
            {
                new ProjectMock("cs1.csproj"),
                new ProjectMock("cs2.csproj"),
                new ProjectMock("cs3.csproj"),
                new ProjectMock("cs4.csproj")
            };

            var logger = new TestLogger();
            var finder = new ConfigurableUnboundProjectFinder
            {
                UnboundProjects = null
            };

            // Act
            var result = BindingProcessImpl.GetProjectsForRulesetBinding(false, allProjects, finder, logger, new NoOpThreadHandler());

            // Assert
            result.Should().BeEmpty();
            logger.AssertOutputStringExists(Strings.Bind_Ruleset_SomeProjectsDoNotNeedToBeUpdated);
            logger.AssertPartialOutputStringExists("cs1.csproj", "cs2.csproj", "cs3.csproj", "cs4.csproj");
        }

        #endregion Tests

        #region Helpers

        private BindingProcessImpl CreateTestSubject(BindCommandArgs bindingArgs = null,
            INuGetBindingOperation nuGetBindingOperation = null,
            IBindingConfigProvider configProvider = null,
            SonarLintMode mode = SonarLintMode.Connected,
            ISonarQubeService sonarQubeService = null,
            IExclusionSettingsStorage exclusionSettingsStorage = null
            )
        {
            bindingArgs = bindingArgs ?? CreateBindCommandArgs();
            nuGetBindingOperation = nuGetBindingOperation ?? new NoOpNuGetBindingOperation(this.host.Logger);
            configProvider = configProvider ?? new Mock<IBindingConfigProvider>().Object;
            exclusionSettingsStorage = exclusionSettingsStorage ?? Mock.Of<IExclusionSettingsStorage>();

            this.host.SonarQubeService = sonarQubeService ?? this.sonarQubeServiceMock.Object;

            var slnBindOperation = new SolutionBindingOperation(this.host, SonarLintMode.LegacyConnected, this.host.Logger);
            var finder = new ConfigurableUnboundProjectFinder();

            return new BindingProcessImpl(this.host, bindingArgs, slnBindOperation, nuGetBindingOperation, finder, configProvider, mode, exclusionSettingsStorage);
        }

        private BindCommandArgs CreateBindCommandArgs(string projectKey = "key", string projectName = "name", ConnectionInformation connection = null)
        {
            connection = connection ?? new ConnectionInformation(new Uri("http://connected"));
            return new BindCommandArgs(projectKey, projectName, connection);
        }

        private void ConfigureSupportedBindingProject(BindingProcessImpl.BindingProcessState internalState, Language language)
        {
            // Mark the language as supported by the host
            host.SupportedPluginLanguages.Add(language);

            // Create a dummy project and add it to the internal state
            var project = new ProjectMock(null);
            switch (language.Id)
            {
                case "VB":
                    project.SetVBProjectKind();
                    break;
                case "CSharp":
                    project.SetCSProjectKind();
                    break;
                default:
                    Assert.Fail($"Test setup error: unknown language: {language}");
                    break;
            }
            internalState.BindingProjects.Add(project);
        }

        private SonarQubeQualityProfile ConfigureQualityProfile(Language language, string profileName)
        {
            var profile = new SonarQubeQualityProfile("", profileName, "", false, DateTime.Now);
            this.sonarQubeServiceMock
                .Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), language.ServerLanguage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            return profile;
        }

        #endregion Helpers
    }
}
