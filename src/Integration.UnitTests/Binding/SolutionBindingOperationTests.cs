/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class SolutionBindingOperationTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableVsOutputWindowPane outputPane;
        private ProjectMock solutionItemsProject;
        private SolutionMock solutionMock;
        private ConfigurableSourceControlledFileSystem sccFileSystem;
        private MockFileSystem fileSystem;

        // Note: currently the project binding saves files using the IRuleSetSerializer.
        // However, solution binding saves files using IBindingConfigFileWithRuleSet.Save(...)
        // -> a test might need to mock both.
        // If/when the project binding switches to IBindingConfigFileWithRuleSet.Save(...)
        // then the tests can be simplified.
        private ConfigurableRuleSetSerializer ruleFS;

        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfo;

        private const string SolutionRoot = @"c:\solution";
        private const string ProjectKey = "key";

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.outputPane = new ConfigurableVsOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane);
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.solutionItemsProject = this.solutionMock.AddOrGetProject("Solution items");
            this.projectSystemHelper.SolutionItemsProject = this.solutionItemsProject;
            this.projectSystemHelper.CurrentActiveSolution = this.solutionMock;
            this.fileSystem = new MockFileSystem();
            this.sccFileSystem = new ConfigurableSourceControlledFileSystem(fileSystem);
            this.ruleFS = new ConfigurableRuleSetSerializer(fileSystem);
            this.ruleSetInfo = new ConfigurableSolutionRuleSetsInformationProvider
            {
                SolutionRootFolder = SolutionRoot
            };

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFileSystem);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.ruleFS);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetInfo);
        }

        #region Tests

        [TestMethod]
        public void SolutionBindingOperation_ArgChecks()
        {
            var connectionInformation = new ConnectionInformation(new Uri("http://valid"));
            var logger = new TestLogger();
            var projectBinderFactory = Mock.Of<IProjectBinderFactory>();
            var folderModifier = Mock.Of<ILegacyConfigFolderItemAdder>();
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(null, connectionInformation, ProjectKey, "name", SonarLintMode.LegacyConnected, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, null, ProjectKey, "name", SonarLintMode.LegacyConnected, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, null, "name", SonarLintMode.LegacyConnected, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, string.Empty, "name", SonarLintMode.LegacyConnected, logger));

            Exceptions.Expect<ArgumentOutOfRangeException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, "123", "name", SonarLintMode.Standalone, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, "123", "name", SonarLintMode.LegacyConnected, null));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, "123", "name", SonarLintMode.Standalone, null, folderModifier, logger, new MockFileSystem()));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, "123", "name", SonarLintMode.Standalone, projectBinderFactory, null, logger, new MockFileSystem()));
            Exceptions.Expect<ArgumentOutOfRangeException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, "123", "name", SonarLintMode.Standalone, projectBinderFactory, folderModifier, logger,null));

            var testSubject = new SolutionBindingOperation(this.serviceProvider, connectionInformation, ProjectKey, "name", SonarLintMode.LegacyConnected, logger);
            testSubject.Should().NotBeNull("Avoid 'testSubject' not used analysis warning");
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets_ArgChecks()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey);

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.RegisterKnownConfigFiles(null));
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey);
            var languageToFileMap = new Dictionary<Language, IBindingConfigFile>();
            languageToFileMap[Language.CSharp] = CreateMockRuleSetConfigFile(Language.CSharp, "c:\\csharp.txt").Object;
            languageToFileMap[Language.VBNET] = CreateMockRuleSetConfigFile(Language.VBNET, "c:\\vbnet.txt").Object;

            // Sanity
            testSubject.RuleSetsInformationMap.Should().BeEmpty("Not expecting any registered rulesets");

            // Act
            testSubject.RegisterKnownConfigFiles(languageToFileMap);

            // Assert
            CollectionAssert.AreEquivalent(languageToFileMap.Keys.ToArray(), testSubject.RuleSetsInformationMap.Keys.ToArray());
            testSubject.RuleSetsInformationMap[Language.CSharp].Should().Be(languageToFileMap[Language.CSharp]);
            testSubject.RuleSetsInformationMap[Language.VBNET].Should().Be(languageToFileMap[Language.VBNET]);
        }

        [TestMethod]
        public void SolutionBindingOperation_GetRuleSetInformation()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey);

            // Test case 1: unknown ruleset map
            var ruleSetMap = new Dictionary<Language, IBindingConfigFile>();
            testSubject.RegisterKnownConfigFiles(ruleSetMap);

            // Act + Assert
            using (new AssertIgnoreScope())
            {
                testSubject.GetBindingConfig(Language.CSharp).Should().BeNull();
            }

            // Test case 2: known ruleset map
            // Arrange
            ruleSetMap[Language.CSharp] = CreateMockRuleSetConfigFile(Language.CSharp, "c:\\csharp.txt").Object;
            ruleSetMap[Language.VBNET] = CreateMockRuleSetConfigFile(Language.VBNET, "c:\\vb.txt").Object;

            testSubject.RegisterKnownConfigFiles(ruleSetMap);
            testSubject.Initialize(new ProjectMock[0], GetQualityProfiles());
            testSubject.Prepare(CancellationToken.None);

            // Act
            string filePath = testSubject.GetBindingConfig(Language.CSharp).FilePath;

            // Assert
            string.IsNullOrWhiteSpace(filePath).Should().BeFalse();
            filePath.Should().Be(testSubject.RuleSetsInformationMap[Language.CSharp].FilePath, "NewRuleSetFilePath is expected to be updated during Prepare and returned now");
        }

        [TestMethod]
        public void SolutionBindingOperation_Initialization_ArgChecks()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey);

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.Initialize(null, GetQualityProfiles()));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.Initialize(new Project[0], null));
        }

        [TestMethod]
        public void SolutionBindingOperation_Initialization()
        {
            // Arrange
            var cs1Project = this.solutionMock.AddOrGetProject("CS1.csproj");
            cs1Project.SetCSProjectKind();
            var cs2Project = this.solutionMock.AddOrGetProject("CS2.csproj");
            cs2Project.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var otherProjectType = this.solutionMock.AddOrGetProject("xxx.proj");
            otherProjectType.ProjectKind = "{" + Guid.NewGuid().ToString() + "}";

            var logger = new TestLogger();

            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey, logger: logger);
            var projects = new[] { cs1Project, vbProject, cs2Project, otherProjectType };

            // Sanity
            testSubject.Binders.Should().BeEmpty("Not expecting any project binders");

            // Act
            testSubject.Initialize(projects, GetQualityProfiles());

            // Assert
            testSubject.SolutionFullPath.Should().Be(Path.Combine(SolutionRoot, "xxx.sln"));
            testSubject.Binders.Should().HaveCount(3, "Should be one per managed project");

            testSubject.Binders.Select(x => ((ProjectBindingOperation)x).ProjectFullPath)
                .Should().BeEquivalentTo("CS1.csproj", "CS2.csproj", "VB.vbproj");

            logger.AssertPartialOutputStringExists("xxx.proj"); // expecting a message about the project that won't be bound, but not the others
            logger.AssertPartialOutputStringDoesNotExist("CS1.csproj");
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare()
        {
            // Arrange
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey);

            var csConfigFile = CreateMockRuleSetConfigFile(Language.CSharp, "c:\\csharp.txt");
            var vbConfigFile = CreateMockRuleSetConfigFile(Language.VBNET, "c:\\vb.txt");
            var ruleSetMap = new Dictionary<Language, IBindingConfigFile>();
            ruleSetMap[Language.CSharp] = csConfigFile.Object;
            ruleSetMap[Language.VBNET] = vbConfigFile.Object;

            testSubject.RegisterKnownConfigFiles(ruleSetMap);
            testSubject.Initialize(projects, GetQualityProfiles());
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            var binder = new ConfigurableBindingOperation();
            testSubject.Binders.Add(binder);
            bool prepareCalledForBinder = false;
            binder.PrepareAction = (ct) => prepareCalledForBinder = true;
            string sonarQubeRulesDirectory = Path.Combine(SolutionRoot, ConfigurableSolutionRuleSetsInformationProvider.DummyLegacyModeFolderName);

            // Sanity
            fileSystem.AllDirectories.Should().NotContain(sonarQubeRulesDirectory);
            testSubject.RuleSetsInformationMap[Language.CSharp].FilePath.Should().Be("c:\\csharp.txt");
            testSubject.RuleSetsInformationMap[Language.VBNET].FilePath.Should().Be("c:\\vb.txt");

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Assert
            prepareCalledForBinder.Should().BeTrue("Expected to propagate the prepare call to binders");
            CheckSaveWasNotCalled(csConfigFile);
            CheckSaveWasNotCalled(vbConfigFile);

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            CheckRuleSetFileWasSaved(csConfigFile, "c:\\csharp.txt");
            CheckRuleSetFileWasSaved(vbConfigFile, "c:\\vb.txt");
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare_Cancellation_DuringBindersPrepare()
        {
            // Arrange
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey);

            var csConfigFile = CreateMockRuleSetConfigFile(Language.CSharp, "c:\\csharp.txt");
            var vbConfigFile = CreateMockRuleSetConfigFile(Language.VBNET, "c:\\vb.txt");
            var languageToFileMap = new Dictionary<Language, IBindingConfigFile>();
            languageToFileMap[Language.CSharp] = csConfigFile.Object;
            languageToFileMap[Language.VBNET] = vbConfigFile.Object;

            testSubject.RegisterKnownConfigFiles(languageToFileMap);
            testSubject.Initialize(projects, GetQualityProfiles());
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool prepareCalledForBinder = false;
            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                testSubject.Binders.Add(new ConfigurableBindingOperation { PrepareAction = (t) => src.Cancel() });
                testSubject.Binders.Add(new ConfigurableBindingOperation { PrepareAction = (t) => prepareCalledForBinder = true });

                // Act
                testSubject.Prepare(src.Token);
            }

            // Assert
            testSubject.RuleSetsInformationMap[Language.CSharp].FilePath.Should().Be("c:\\csharp.txt");
            testSubject.RuleSetsInformationMap[Language.VBNET].FilePath.Should().Be("c:\\vb.txt");
            prepareCalledForBinder.Should().BeFalse("Expected to be canceled as soon as possible i.e. after the first binder");

            CheckSaveWasNotCalled(csConfigFile);
            CheckSaveWasNotCalled(vbConfigFile);
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare_Cancellation_BeforeBindersPrepare()
        {
            // Arrange
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey);

            var csConfigFile = CreateMockRuleSetConfigFile(Language.CSharp, "c:\\csharp.txt");
            var vbConfigFile = CreateMockRuleSetConfigFile(Language.VBNET, "c:\\vb.txt");
            var ruleSetMap = new Dictionary<Language, IBindingConfigFile>();
            ruleSetMap[Language.CSharp] = csConfigFile.Object;
            ruleSetMap[Language.VBNET] = vbConfigFile.Object;

            testSubject.RegisterKnownConfigFiles(ruleSetMap);
            testSubject.Initialize(projects, GetQualityProfiles());
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool prepareCalledForBinder = false;
            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                testSubject.Binders.Add(new ConfigurableBindingOperation { PrepareAction = (t) => prepareCalledForBinder = true });
                src.Cancel();

                // Act
                testSubject.Prepare(src.Token);
            }

            // Assert
            testSubject.RuleSetsInformationMap[Language.CSharp].FilePath.Should().NotBeNull("Expected to be set before Prepare is called");
            testSubject.RuleSetsInformationMap[Language.VBNET].FilePath.Should().NotBeNull("Expected to be set before Prepare is called");
            prepareCalledForBinder.Should().BeFalse("Expected to be canceled as soon as possible i.e. before the first binder");
            CheckSaveWasNotCalled(csConfigFile);
            CheckSaveWasNotCalled(vbConfigFile);
        }

        [TestMethod]
        public void SolutionBindingOperation_CommitSolutionBinding_LegacyConnectedMode()
        {
            // Act & Assert
            var expectedFilePath = $"c:\\{Guid.NewGuid()}.txt"; 
            ExecuteCommitSolutionBindingTest(SonarLintMode.LegacyConnected, expectedFilePath);

            this.solutionItemsProject.Files.ContainsKey(expectedFilePath).Should().BeTrue("Ruleset was expected to be added to solution items when in legacy mode");
            fileSystem.GetFile(expectedFilePath).Should().NotBe(null); // check the file was saved
        }

        [TestMethod]
        public void SolutionBindingOperation_CommitSolutionBinding_ConnectedMode()
        {
            // Act & Assert
            var expectedFilePath = $"c:\\{Guid.NewGuid()}.txt";
            ExecuteCommitSolutionBindingTest(SonarLintMode.Connected, expectedFilePath);

            this.solutionItemsProject.Files.Count.Should().Be(0, "Not expecting any items to be added to the solution in new connected mode");
            fileSystem.GetFile(expectedFilePath).Should().NotBe(null); // check the file was saved
        }

        private void ExecuteCommitSolutionBindingTest(SonarLintMode bindingMode, string expectedFilePath)
        {
            // Arrange
            var configPersister = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationPersister), configPersister);
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var projects = new[] { csProject };

            var connectionInformation = new ConnectionInformation(new Uri("http://xyz"));
            SolutionBindingOperation testSubject = this.CreateTestSubject(ProjectKey, connectionInformation, bindingMode);

            var configFileMock = CreateMockRuleSetConfigFile(Language.CSharp, expectedFilePath);
            var languageToFileMap = new Dictionary<Language, IBindingConfigFile>()
            {
                { Language.CSharp, configFileMock.Object }
            };

            testSubject.RegisterKnownConfigFiles(languageToFileMap);
            var profiles = GetQualityProfiles();

            DateTime expectedTimeStamp = DateTime.Now;
            profiles[Language.CSharp] = new SonarQubeQualityProfile("expected profile Key", "", "", false, expectedTimeStamp);
            testSubject.Initialize(projects, profiles);
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool commitCalledForBinder = false;
            testSubject.Binders.Add(new ConfigurableBindingOperation { CommitAction = () => commitCalledForBinder = true });
            testSubject.Prepare(CancellationToken.None);

            // Sanity
            configPersister.SavedProject.Should().BeNull();

            // Act
            var commitResult = testSubject.CommitSolutionBinding();

            // Assert
            commitResult.Should().BeTrue();
            commitCalledForBinder.Should().BeTrue();

            configPersister.SavedProject.Should().NotBeNull();
            configPersister.SavedMode.Should().Be(bindingMode);

            var savedProject = configPersister.SavedProject;
            savedProject.ServerUri.Should().Be(connectionInformation.ServerUri);
            savedProject.Profiles.Should().HaveCount(1);
            savedProject.Profiles[Language.CSharp].ProfileKey.Should().Be("expected profile Key");
            savedProject.Profiles[Language.CSharp].ProfileTimestamp.Should().Be(expectedTimeStamp);
        }

        #endregion Tests

        #region Helpers

        private SolutionBindingOperation CreateTestSubject(string projectKey,
            ConnectionInformation connection = null,
            SonarLintMode bindingMode = SonarLintMode.LegacyConnected,
            ILogger logger = null)
        {
            return new SolutionBindingOperation(serviceProvider,
                connection ?? new ConnectionInformation(new Uri("http://host")),
                projectKey,
                projectKey,
                bindingMode,
                new ProjectBinderFactory(serviceProvider, fileSystem),
                new LegacyConfigFolderItemAdder(serviceProvider, fileSystem),
                logger ?? new TestLogger(),
                fileSystem);
        }

        private static Dictionary<Language, SonarQubeQualityProfile> GetQualityProfiles()
        {
            return new Dictionary<Language, SonarQubeQualityProfile>();
        }

        private Mock<IBindingConfigFileWithRuleset> CreateMockRuleSetConfigFile(Language language, string expectedFilePath)
        {
            var rulesetConfig = new Mock<IBindingConfigFileWithRuleset>();
            rulesetConfig.Setup(x => x.RuleSet)
                .Returns(new RuleSet(language.Name));

            rulesetConfig.SetupGet(x => x.FilePath)
                .Returns(expectedFilePath);

            // Simulate an update to the scc file system on Save (prevents an assertion
            // in the product code).
            rulesetConfig.Setup(x => x.Save())
                .Callback(() =>
                {
                    fileSystem.AddFile(expectedFilePath, new MockFileData(""));
                });

            return rulesetConfig;
        }

        private static void CheckRuleSetFileWasSaved(Mock<IBindingConfigFileWithRuleset> mock, string expectedFileName)
        {
            mock.Verify(x => x.Save(), Times.Once);
        }

        private static void CheckSaveWasNotCalled(Mock<IBindingConfigFileWithRuleset> mock)
            => mock.Verify(x => x.Save(), Times.Never);



        #endregion Helpers
    }
}
