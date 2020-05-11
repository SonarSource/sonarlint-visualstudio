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
        private Mock<IProjectBinderFactory> projectBinderFactoryMock;

        private const string SolutionRoot = @"c:\solution";

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

            projectBinderFactoryMock = new Mock<IProjectBinderFactory>();
        }

        #region Tests

        [TestMethod]
        public void SolutionBindingOperation_ArgChecks()
        {
            var logger = new TestLogger();
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(null, SonarLintMode.LegacyConnected, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, SonarLintMode.LegacyConnected, null));

            var testSubject = new SolutionBindingOperation(serviceProvider, SonarLintMode.LegacyConnected, logger);
            testSubject.Should().NotBeNull("Avoid 'testSubject' not used analysis warning");
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets_ArgChecks()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.RegisterKnownConfigFiles(null));
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject();
            var languageToFileMap = new Dictionary<Language, IBindingConfig>();
            languageToFileMap[Language.CSharp] = CreateMockConfigFile("c:\\csharp.txt").Object;
            languageToFileMap[Language.VBNET] = CreateMockConfigFile("c:\\vbnet.txt").Object;

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
            SolutionBindingOperation testSubject = this.CreateTestSubject();

            // Test case 1: unknown ruleset map
            var ruleSetMap = new Dictionary<Language, IBindingConfig>();
            testSubject.RegisterKnownConfigFiles(ruleSetMap);

            // Act + Assert
            using (new AssertIgnoreScope())
            {
                testSubject.GetBindingConfig(Language.CSharp).Should().BeNull();
            }

            // Test case 2: known ruleset map
            // Arrange
            ruleSetMap[Language.CSharp] = CreateMockConfigFile("c:\\csharp.txt").Object;
            ruleSetMap[Language.VBNET] = CreateMockConfigFile("c:\\vb.txt").Object;

            testSubject.RegisterKnownConfigFiles(ruleSetMap);
            testSubject.Initialize(new ProjectMock[0]);
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
            SolutionBindingOperation testSubject = this.CreateTestSubject();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.Initialize(null));
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

            var testSubject = CreateTestSubject();
            var projects = new[] { cs1Project, vbProject, cs2Project, otherProjectType };

            // Sanity
            testSubject.Binders.Should().BeEmpty("Not expecting any project binders");

            // Act
            testSubject.Initialize(projects);

            // Assert
            testSubject.SolutionFullPath.Should().Be(Path.Combine(SolutionRoot, "xxx.sln"));
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare_ProjectBindersAreCalled()
        {
            // Arrange
            var csProject = solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var csConfigFile = CreateMockConfigFile("c:\\csharp.txt");
            var csBinder = new Mock<IProjectBinder>();
            var csCommitAction = new Mock<BindProject>();

            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var vbConfigFile = CreateMockConfigFile("c:\\vb.txt");
            var vbBinder = new Mock<IProjectBinder>();
            var vbCommitAction = new Mock<BindProject>();

            projectBinderFactoryMock.Setup(x => x.Get(csProject)).Returns(csBinder.Object);
            projectBinderFactoryMock.Setup(x => x.Get(vbProject)).Returns(vbBinder.Object);

            csBinder
                .Setup(x => x.GetBindAction(csConfigFile.Object, csProject, CancellationToken.None))
                .Returns(csCommitAction.Object);

            vbBinder.Setup(x => x.GetBindAction(vbConfigFile.Object, vbProject, CancellationToken.None))
                .Returns(vbCommitAction.Object);

            var projects = new[] { csProject, vbProject };

            var testSubject = CreateTestSubject();

            var ruleSetMap = new Dictionary<Language, IBindingConfig>
            {
                [Language.CSharp] = csConfigFile.Object,
                [Language.VBNET] = vbConfigFile.Object
            };

            testSubject.RegisterKnownConfigFiles(ruleSetMap);
            testSubject.Initialize(projects);
            var sonarQubeRulesDirectory = Path.Combine(SolutionRoot, ConfigurableSolutionRuleSetsInformationProvider.DummyLegacyModeFolderName);

            // Sanity
            fileSystem.AllDirectories.Should().NotContain(sonarQubeRulesDirectory);
            testSubject.RuleSetsInformationMap[Language.CSharp].FilePath.Should().Be("c:\\csharp.txt");
            testSubject.RuleSetsInformationMap[Language.VBNET].FilePath.Should().Be("c:\\vb.txt");

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Assert
            testSubject.Binders.Should().HaveCount(2, "Should be one per managed project");

            csBinder.Verify(x=> x.GetBindAction(csConfigFile.Object, csProject, CancellationToken.None), Times.Once);
            vbBinder.Verify(x=> x.GetBindAction(vbConfigFile.Object, vbProject, CancellationToken.None), Times.Once);

            csCommitAction.VerifyNoOtherCalls();
            vbCommitAction.VerifyNoOtherCalls();

            testSubject.Binders.First().Should().BeSameAs(csCommitAction.Object);
            testSubject.Binders.Last().Should().BeSameAs(vbCommitAction.Object);

            CheckSaveWasNotCalled(csConfigFile);
            CheckSaveWasNotCalled(vbConfigFile);

            // Act (write pending)
            sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            CheckRuleSetFileWasSaved(csConfigFile);
            CheckRuleSetFileWasSaved(vbConfigFile);
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare_Cancellation_ProjectBindersAreNotCalled()
        {
            // Arrange
            var csProject = solutionMock.AddOrGetProject("CS.csproj");
            var csConfigFile = CreateMockConfigFile("c:\\csharp.txt");
            var csBinder = new Mock<IProjectBinder>();

            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            var vbConfigFile = CreateMockConfigFile("c:\\vb.txt");
            var vbBinder = new Mock<IProjectBinder>();

            projectBinderFactoryMock.Setup(x => x.Get(csProject)).Returns(csBinder.Object);
            projectBinderFactoryMock.Setup(x => x.Get(vbProject)).Returns(vbBinder.Object);

            var projects = new[] { csProject, vbProject };

            var testSubject = CreateTestSubject();

            var languageToFileMap = new Dictionary<Language, IBindingConfig>();
            languageToFileMap[Language.CSharp] = csConfigFile.Object;
            languageToFileMap[Language.VBNET] = vbConfigFile.Object;

            testSubject.RegisterKnownConfigFiles(languageToFileMap);
            testSubject.Initialize(projects);

            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                src.Cancel();
                // Act
                testSubject.Prepare(src.Token);
            }

            // Assert
            testSubject.Binders.Count.Should().Be(0);
            csBinder.VerifyNoOtherCalls();
            vbBinder.VerifyNoOtherCalls();

            testSubject.RuleSetsInformationMap[Language.CSharp].FilePath.Should().Be("c:\\csharp.txt");
            testSubject.RuleSetsInformationMap[Language.VBNET].FilePath.Should().Be("c:\\vb.txt");

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
            var csProject = solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var csConfigFile = CreateMockConfigFile(expectedFilePath);
            var csBinder = new Mock<IProjectBinder>();
            var csBinderCommitAction = new Mock<BindProject>();

            projectBinderFactoryMock.Setup(x => x.Get(csProject)).Returns(csBinder.Object);

            csBinder
                .Setup(x => x.GetBindAction(csConfigFile.Object, csProject, CancellationToken.None))
                .Returns(csBinderCommitAction.Object);

            var projects = new[] { csProject };

            var connectionInformation = new ConnectionInformation(new Uri("http://xyz"));
            SolutionBindingOperation testSubject = this.CreateTestSubject(bindingMode);

            var languageToFileMap = new Dictionary<Language, IBindingConfig>()
            {
                { Language.CSharp, csConfigFile.Object }
            };

            testSubject.RegisterKnownConfigFiles(languageToFileMap);

            DateTime expectedTimeStamp = DateTime.Now;
            testSubject.Initialize(projects);
            testSubject.Prepare(CancellationToken.None);

            // Act
            var commitResult = testSubject.CommitSolutionBinding();

            // Assert
            commitResult.Should().BeTrue();
            csBinderCommitAction.Verify(x=> x(), Times.Once);
        }

        #endregion Tests

        #region Helpers

        private SolutionBindingOperation CreateTestSubject(SonarLintMode bindingMode = SonarLintMode.LegacyConnected)
        {
            return new SolutionBindingOperation(serviceProvider,
                bindingMode,
                projectBinderFactoryMock.Object,
                new LegacyConfigFolderItemAdder(serviceProvider, fileSystem),
                fileSystem);
        }

        private Mock<IBindingConfig> CreateMockConfigFile(string expectedFilePath)
        {
            var configFile = new Mock<IBindingConfig>();
            
            configFile.SetupGet(x => x.FilePath)
                .Returns(expectedFilePath);

            // Simulate an update to the scc file system on Save (prevents an assertion
            // in the product code).
            configFile.Setup(x => x.Save())
                .Callback(() =>
                {
                    fileSystem.AddFile(expectedFilePath, new MockFileData(""));
                });

            return configFile;
        }

        private static void CheckRuleSetFileWasSaved(Mock<IBindingConfig> mock)
        {
            mock.Verify(x => x.Save(), Times.Once);
        }

        private static void CheckSaveWasNotCalled(Mock<IBindingConfig> mock)
            => mock.Verify(x => x.Save(), Times.Never);



        #endregion Helpers
    }
}
