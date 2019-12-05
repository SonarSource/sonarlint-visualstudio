/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Linq;
using System.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
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
        private ConfigurableProjectSystemFilter projectFilter;
        private ConfigurableVsOutputWindowPane outputPane;
        private ProjectMock solutionItemsProject;
        private SolutionMock solutionMock;
        private ConfigurableSourceControlledFileSystem sccFileSystem;
        private ConfigurableRuleSetSerializer ruleFS;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfo;

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
            this.projectFilter = new ConfigurableProjectSystemFilter();
            this.solutionItemsProject = this.solutionMock.AddOrGetProject("Solution items");
            this.projectSystemHelper.SolutionItemsProject = this.solutionItemsProject;
            this.projectSystemHelper.CurrentActiveSolution = this.solutionMock;
            this.sccFileSystem = new ConfigurableSourceControlledFileSystem();
            this.ruleFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
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
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(null, connectionInformation, "key", "name", SonarLintMode.LegacyConnected, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, null, "key", "name", SonarLintMode.LegacyConnected, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, null, "name", SonarLintMode.LegacyConnected, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, string.Empty, "name", SonarLintMode.LegacyConnected, logger));

            Exceptions.Expect<ArgumentOutOfRangeException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, "123", "name", SonarLintMode.Standalone, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, "123", "name", SonarLintMode.LegacyConnected, null));

            var testSubject = new SolutionBindingOperation(this.serviceProvider, connectionInformation, "key", "name", SonarLintMode.LegacyConnected, logger);
            testSubject.Should().NotBeNull("Avoid 'testSubject' not used analysis warning");
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets_ArgChecks()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.RegisterKnownRuleSets(null));
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var ruleSetMap = new Dictionary<Language, IRulesConfigurationFile>();
            ruleSetMap[Language.CSharp] = new DotNetRulesConfigurationFile(new RuleSet("cs"));
            ruleSetMap[Language.VBNET] = new DotNetRulesConfigurationFile(new RuleSet("vb"));

            // Sanity
            testSubject.RuleSetsInformationMap.Should().BeEmpty("Not expecting any registered rulesets");

            // Act
            testSubject.RegisterKnownRuleSets(ruleSetMap);

            // Assert
            CollectionAssert.AreEquivalent(ruleSetMap.Keys.ToArray(), testSubject.RuleSetsInformationMap.Keys.ToArray());
            testSubject.RuleSetsInformationMap[Language.CSharp].RulesConfigurationFile.Should().Be(ruleSetMap[Language.CSharp]);
            testSubject.RuleSetsInformationMap[Language.VBNET].RulesConfigurationFile.Should().Be(ruleSetMap[Language.VBNET]);
        }

        [TestMethod]
        public void SolutionBindingOperation_GetRuleSetInformation()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Test case 1: unknown ruleset map
            // Act + Assert
            using (new AssertIgnoreScope())
            {
                testSubject.GetRuleSetInformation(Language.CSharp).Should().BeNull();
            }

            // Test case 2: known ruleset map
            // Arrange
            var ruleSetMap = new Dictionary<Language, IRulesConfigurationFile>();
            ruleSetMap[Language.CSharp] = new DotNetRulesConfigurationFile(new RuleSet("cs"));
            ruleSetMap[Language.VBNET] = new DotNetRulesConfigurationFile(new RuleSet("vb"));

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize(new ProjectMock[0], GetQualityProfiles());
            testSubject.Prepare(CancellationToken.None);

            // Act
            string filePath = testSubject.GetRuleSetInformation(Language.CSharp).NewRuleSetFilePath;

            // Assert
            string.IsNullOrWhiteSpace(filePath).Should().BeFalse();
            filePath.Should().Be(testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath, "NewRuleSetFilePath is expected to be updated during Prepare and returned now");
        }

        [TestMethod]
        public void SolutionBindingOperation_Initialization_ArgChecks()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

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

            SolutionBindingOperation testSubject = this.CreateTestSubject("key", logger: logger);
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
        public void SolutionBindingOperation_IsInitializationRequired()
        {
            // 1. C# -> binding is required
            var csProject = new ProjectMock("c:\\foo.proj");
            csProject.SetCSProjectKind();

            SolutionBindingOperation.IsProjectBindingRequired(csProject).Should().BeTrue();

            // 2. VB.NET -> binding is required
            var vbProject = new ProjectMock("c:\\foo.proj");
            vbProject.SetVBProjectKind();

            SolutionBindingOperation.IsProjectBindingRequired(vbProject).Should().BeTrue();

            // 3. Other -> binding is not required
            var otherProject = new ProjectMock("c:\\foo.proj");
            otherProject.ProjectKind = "{" + Guid.NewGuid().ToString() + "}";

            SolutionBindingOperation.IsProjectBindingRequired(otherProject).Should().BeFalse();
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

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<Language, IRulesConfigurationFile>();
            ruleSetMap[Language.CSharp] = new DotNetRulesConfigurationFile(new RuleSet("cs"));
            ruleSetMap[Language.VBNET] = new DotNetRulesConfigurationFile(new RuleSet("vb"));

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize(projects, GetQualityProfiles());
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            var binder = new ConfigurableBindingOperation();
            testSubject.Binders.Add(binder);
            bool prepareCalledForBinder = false;
            binder.PrepareAction = (ct) => prepareCalledForBinder = true;
            string sonarQubeRulesDirectory = Path.Combine(SolutionRoot, ConfigurableSolutionRuleSetsInformationProvider.DummyLegacyModeFolderName);

            var csharpRulesetPath = Path.Combine(sonarQubeRulesDirectory, "keyCSharp.ruleset");
            var vbRulesetPath = Path.Combine(sonarQubeRulesDirectory, "keyVB.ruleset");

            // Sanity
            this.sccFileSystem.directories.Should().NotContain(sonarQubeRulesDirectory);
            testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath.Should().Be(csharpRulesetPath);
            testSubject.RuleSetsInformationMap[Language.VBNET].NewRuleSetFilePath.Should().Be(vbRulesetPath);

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Assert
            this.sccFileSystem.directories.Should().NotContain(sonarQubeRulesDirectory);
            prepareCalledForBinder.Should().BeTrue("Expected to propagate the prepare call to binders");
            this.sccFileSystem.files.Should().NotContainKey(csharpRulesetPath);
            this.sccFileSystem.files.Should().NotContainKey(vbRulesetPath);

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            this.sccFileSystem.files.Should().ContainKey(csharpRulesetPath);
            this.sccFileSystem.files.Should().ContainKey(vbRulesetPath);
            this.sccFileSystem.directories.Should().Contain(sonarQubeRulesDirectory);
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

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var ruleSetMap = new Dictionary<Language, IRulesConfigurationFile>();
            ruleSetMap[Language.CSharp] = new DotNetRulesConfigurationFile(new RuleSet("cs"));
            ruleSetMap[Language.VBNET] = new DotNetRulesConfigurationFile(new RuleSet("vb"));

            testSubject.RegisterKnownRuleSets(ruleSetMap);
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
            string expectedSolutionFolder = Path.Combine(SolutionRoot, ConfigurableSolutionRuleSetsInformationProvider.DummyLegacyModeFolderName);
            testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath.Should().Be(Path.Combine(expectedSolutionFolder, "keyCSharp.ruleset"));
            testSubject.RuleSetsInformationMap[Language.VBNET].NewRuleSetFilePath.Should().Be(Path.Combine(expectedSolutionFolder, "keyVB.ruleset"));
            prepareCalledForBinder.Should().BeFalse("Expected to be canceled as soon as possible i.e. after the first binder");
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

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<Language, IRulesConfigurationFile>();
            ruleSetMap[Language.CSharp] = new DotNetRulesConfigurationFile(new RuleSet("cs"));
            ruleSetMap[Language.VBNET] = new DotNetRulesConfigurationFile(new RuleSet("vb"));

            testSubject.RegisterKnownRuleSets(ruleSetMap);
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
            testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath.Should().NotBeNull("Expected to be set before Prepare is called");
            testSubject.RuleSetsInformationMap[Language.VBNET].NewRuleSetFilePath.Should().NotBeNull("Expected to be set before Prepare is called");
            prepareCalledForBinder.Should().BeFalse("Expected to be canceled as soon as possible i.e. before the first binder");
        }

        [TestMethod]
        public void SolutionBindingOperation_CommitSolutionBinding_LegacyConnectedMode()
        {
            // Act & Assert
            ExecuteCommitSolutionBindingTest(SonarLintMode.LegacyConnected);

            var expectedRuleset = Path.Combine(SolutionRoot, ConfigurableSolutionRuleSetsInformationProvider.DummyLegacyModeFolderName, "keyCSharp.ruleset");
            this.solutionItemsProject.Files.ContainsKey(expectedRuleset).Should().BeTrue("Ruleset was expected to be added to solution items when in legacy mode");
        }

        [TestMethod]
        public void SolutionBindingOperation_CommitSolutionBinding_ConnectedMode()
        {
            // Act & Assert
            ExecuteCommitSolutionBindingTest(SonarLintMode.Connected);

            this.solutionItemsProject.Files.Count.Should().Be(0, "Not expecting any items to be added to the solution in new connected mode");
        }

        private void ExecuteCommitSolutionBindingTest(SonarLintMode bindingMode)
        {
            // Arrange
            var configProvider = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationProvider), configProvider);
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var projects = new[] { csProject };

            var connectionInformation = new ConnectionInformation(new Uri("http://xyz"));
            SolutionBindingOperation testSubject = this.CreateTestSubject("key", connectionInformation, bindingMode);

            var ruleSetMap = new Dictionary<Language, IRulesConfigurationFile>()
            {
                { Language.CSharp, new DotNetRulesConfigurationFile(new RuleSet("cs")) }
            };
            
            testSubject.RegisterKnownRuleSets(ruleSetMap);
            var profiles = GetQualityProfiles();

            DateTime expectedTimeStamp = DateTime.Now;
            profiles[Language.CSharp] = new SonarQubeQualityProfile("expected profile Key", "", "", false, expectedTimeStamp);
            testSubject.Initialize(projects, profiles);
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool commitCalledForBinder = false;
            testSubject.Binders.Add(new ConfigurableBindingOperation { CommitAction = () => commitCalledForBinder = true });
            testSubject.Prepare(CancellationToken.None);

            // Sanity
            configProvider.SavedConfiguration.Should().BeNull();

            // Act
            var commitResult = testSubject.CommitSolutionBinding();

            // Assert
            commitResult.Should().BeTrue();
            commitCalledForBinder.Should().BeTrue();

            configProvider.SavedConfiguration.Should().NotBeNull();
            configProvider.SavedConfiguration.Mode.Should().Be(bindingMode);

            var savedProject = configProvider.SavedConfiguration.Project;
            savedProject.ServerUri.Should().Be(connectionInformation.ServerUri);
            savedProject.Profiles.Should().HaveCount(1);
            savedProject.Profiles[Language.CSharp].ProfileKey.Should().Be("expected profile Key");
            savedProject.Profiles[Language.CSharp].ProfileTimestamp.Should().Be(expectedTimeStamp);
        }

        [TestMethod]
        public void SolutionBindingOperation_RuleSetInformation_Ctor_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInformation(Language.CSharp, null));
        }

        #endregion Tests

        #region Helpers

        private SolutionBindingOperation CreateTestSubject(string projectKey,
            ConnectionInformation connection = null,
            SonarLintMode bindingMode = SonarLintMode.LegacyConnected,
            ILogger logger = null)
        {
            return new SolutionBindingOperation(this.serviceProvider,
                connection ?? new ConnectionInformation(new Uri("http://host")),
                projectKey,
                projectKey,
                bindingMode,
                logger ?? new TestLogger());
        }

        private static Dictionary<Language, SonarQubeQualityProfile> GetQualityProfiles()
        {
            return new Dictionary<Language, SonarQubeQualityProfile>();
        }

        #endregion Helpers
    }
}
