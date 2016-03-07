//-----------------------------------------------------------------------
// <copyright file="BindingWorkflowTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BindingWorkflowTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableVsGeneralOutputWindowPane outputWindowPane;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.outputWindowPane = new ConfigurableVsGeneralOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindowPane);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
        }

        #region Tests

        [TestMethod]
        public void BindingWorkflow_VerifyServerPlugin_HasCSharpPlugin_DoesNotAbort()
        {
            // Setup
            var testSubject = CreateTestSubject();
            var progressExecEvents = new ConfigurableProgressStepExecutionEvents();
            var progressController = new ConfigurableProgressController();
            var plugin = new ServerPlugin { Key = ServerPlugin.CSharpPluginKey, Version = ServerPlugin.CSharpPluginMinimumVersion };
            this.sonarQubeService.RegisterServerPlugin(plugin);

            // Act
            testSubject.VerifyServerPlugins(progressController, CancellationToken.None, progressExecEvents);

            // Verify
            progressController.AssertNumberOfAbortRequests(0);
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void BindingWorkflow_VerifyServerPlugin_MissingCSharpPlugin_AbortsWorkflow()
        {
            // Setup
            var testSubject = CreateTestSubject();
            var progressExecEvents = new ConfigurableProgressStepExecutionEvents();
            var progressController = new ConfigurableProgressController();
            string expectedErrorMsg = string.Format(CultureInfo.CurrentCulture, Strings.ServerDoesNotHaveCorrectVersionOfCSharpPlugin, ServerPlugin.CSharpPluginMinimumVersion);

            // Act
            testSubject.VerifyServerPlugins(progressController, CancellationToken.None, progressExecEvents);

            // Verify
            progressController.AssertNumberOfAbortRequests(1);
            this.outputWindowPane.AssertOutputStrings(1);
            this.outputWindowPane.AssertOutputStrings(expectedErrorMsg);
        }

        [TestMethod]
        public void BindingWorkflow_DownloadQualityProfile_Success()
        {
            // Setup
            BindingWorkflow testSubject = this.CreateTestSubject();
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "Key1", "Key2" });
            var nugetPackages = new[] { new PackageName("myPackageId", new SemanticVersion("1.0.0")) };
            var additionalFiles = new[] { new AdditionalFile { FileName = "abc.xml", Content = new byte[] { 1, 2, 3 } } };
            RoslynExportProfile export = RoslynExportProfileHelper.CreateExport(ruleSet, nugetPackages, additionalFiles);

            string language = "lang";
            this.ConfigureProfileExport(testSubject, export, language, RuleSetGroup.VB);

            // Act
            testSubject.DownloadQualityProfile(controller, CancellationToken.None, notifications, new[] { language });

            // Verify
            RuleSetAssert.AreEqual(ruleSet, testSubject.Rulesets[RuleSetGroup.VB], "Unexpected rule set");
            VerifyNuGetPackgesDownloaded(nugetPackages, testSubject);
            this.outputWindowPane.AssertOutputStrings(0);
            controller.AssertNumberOfAbortRequests(0);
            notifications.AssertProgress(
                0.0,
                1.0,
                1.0);
            notifications.AssertProgressMessages(
                string.Format(CultureInfo.CurrentCulture, Strings.DownloadingQualityProfileProgressMessage, language),
                string.Empty,
                Strings.QualityProfileDownloadedSuccessfulMessage);
        }

        [TestMethod]
        public void BindingWorkflow_DownloadQualityProfile_Failure()
        {
            // Setup
            BindingWorkflow testSubject = this.CreateTestSubject();
            ConfigurableProgressController controller = new ConfigurableProgressController();
            string language = "lang";
            this.ConfigureProfileExport(testSubject, null, language, RuleSetGroup.VB);

            // Act
            testSubject.DownloadQualityProfile(controller, CancellationToken.None, new ConfigurableProgressStepExecutionEvents(), new[] { language });

            // Verify
            Assert.IsFalse(testSubject.Rulesets.ContainsKey(RuleSetGroup.VB), "Not expecting any rules for this language");
            Assert.IsFalse(testSubject.Rulesets.ContainsKey(RuleSetGroup.CSharp), "Not expecting any rules");
            this.outputWindowPane.AssertOutputStrings(1);
            controller.AssertNumberOfAbortRequests(1);
        }

        [TestMethod]
        public void BindingWorkflow_SetSolutionRuleSet()
        {
            // Setup
            const string projectName = "My Awesome Project";
            const string projectKey = "MyAweProj";
            const string solutionRoot = @"X:\MySolution";

            var projectInfo = new ProjectInformation { Name = projectName, Key = projectKey };
            var solution = new SolutionMock(null, Path.Combine(solutionRoot, "Solution.sln"));
            RuleSetGroup group = RuleSetGroup.CSharp;

            var expectedRuleSetPath = Path.Combine
            (
                solutionRoot,
                Constants.SonarQubeManagedFolderName,
                projectKey + group.ToString()
            ) + "." + RuleSetWriter.FileExtension;

            RuleSet expectedRuleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "rule1", "rule2" });

            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            BindingWorkflow testSubject = this.CreateTestSubject(projectInfo, fileSystem);
            testSubject.Rulesets[group] = expectedRuleSet;

            // Act
            testSubject.SetSolutionRuleSet(group, solution.FilePath);

            // Verify
            Assert.AreEqual(expectedRuleSetPath, testSubject.SolutionRulesetPaths[group], "Rule set path should have been set");
            fileSystem.AssertFileExists(expectedRuleSetPath);
            fileSystem.AssertRuleSetsAreEqual(expectedRuleSetPath, expectedRuleSet);
        }

        [TestMethod]
        public void BindingWorkflow_UpdateProjectRuleSet_ExistingRuleSet()
        {
            // Setup
            const string solutionRuleSetPath = @"X:\SolutionDir\SolutionRuleSets\sonar1.ruleset";
            const string solutionRuleSetInclude = @"..\SolutionRuleSets\sonar1.ruleset";

            const string projectName = "My Awesome Project";
            const string projectRoot = @"X:\SolutionDir\ProjectDir";
            const string configurationName = "Happy";
            string projectFullPath = Path.Combine(projectRoot, projectName + ".proj");

            string existingRuleSetPath = Path.Combine(projectRoot, "mycustomruleset.ruleset");
            var existingRuleSet = new RuleSet(Constants.RuleSetName);
            existingRuleSet.Rules.Add(new RuleReference("testId", "testNs", "42", RuleAction.Default));

            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            fileSystem.AddRuleSetFile(existingRuleSetPath, existingRuleSet);

            string expectedRuleSetPath = existingRuleSetPath;
            var expectedRuleSet = new RuleSet(Constants.RuleSetName);
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionRuleSetInclude, RuleAction.Default));
            expectedRuleSet.Rules.Add(new RuleReference("testId", "testNs", "42", RuleAction.Default));

            BindingWorkflow testSubject = this.CreateTestSubject(new ProjectInformation(), fileSystem);
            RuleSetGroup group = RuleSetGroup.VB;
            testSubject.SolutionRulesetPaths[group] = solutionRuleSetPath;

            // Act
            testSubject.UpdateProjectRuleSet(group, projectFullPath, configurationName, existingRuleSetPath);

            // Verify
            fileSystem.AssertFileExists(expectedRuleSetPath);
            fileSystem.AssertRuleSetsAreEqual(expectedRuleSetPath, expectedRuleSet);
        }

        [TestMethod]
        public void BindingWorkflow_UpdateProjectRuleSet_NoExistingRuleSet()
        {
            // Setup
            const string solutionRuleSetPath = @"X:\SolutionDir\SolutionRuleSets\sonar1.ruleset";
            const string solutionRuleSetInclude = @"..\SolutionRuleSets\sonar1.ruleset";

            const string projectName = "My Awesome Project";
            const string projectRoot = @"X:\SolutionDir\ProjectDir";
            const string configurationName = "Happy";
            string projectFullPath = Path.Combine(projectRoot, projectName + ".proj");

            var expectedRuleSetPath = Path.Combine(projectRoot, projectName + "." + configurationName + "." + RuleSetWriter.FileExtension);
            var expectedRuleSet = new RuleSet(Constants.RuleSetName);
            expectedRuleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionRuleSetInclude, RuleAction.Default));

            var fileSystem = new ConfigurableRuleSetGenerationFileSystem();
            BindingWorkflow testSubject = this.CreateTestSubject(new ProjectInformation(), fileSystem);
            RuleSetGroup group = RuleSetGroup.CSharp;
            testSubject.SolutionRulesetPaths[group] = solutionRuleSetPath;

            // Act
            testSubject.UpdateProjectRuleSet(group, projectFullPath, configurationName, null);

            // Verify
            fileSystem.AssertFileExists(expectedRuleSetPath);
            fileSystem.AssertRuleSetsAreEqual(expectedRuleSetPath, expectedRuleSet);
        }

        [TestMethod]
        public void BindingWorkflow_InstallPackages()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            ProjectMock project1 = new ProjectMock("project1");
            ProjectMock project2 = new ProjectMock("project2");

            var nugetPackage = new PackageName("mypackage", new SemanticVersion("1.1.0"));
            var packages = new[] { nugetPackage };

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, packages, project1, project2);

            // Act
            testSubject.InstallPackages(new ConfigurableProgressController(), CancellationToken.None, progressEvents);

            // Verify
            packageInstaller.AssertInstalledPackages(project1, packages);
            packageInstaller.AssertInstalledPackages(project2, packages);
            progressEvents.AssertProgressMessages(
                string.Format(CultureInfo.CurrentCulture, Strings.EnsuringNugetPackagesProgressMessage, nugetPackage.Id, ((Project)project1).Name),
                string.Empty,
                string.Format(CultureInfo.CurrentCulture, Strings.EnsuringNugetPackagesProgressMessage, nugetPackage.Id, ((Project)project2).Name),
                string.Empty);
            progressEvents.AssertProgress(
                0,
                .5,
                .5,
                1.0);
        }

        [TestMethod]
        public void BindingWorkflow_InstallPackages_Cancellation()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var cts = new CancellationTokenSource();

            ProjectMock project1 = new ProjectMock("project1");
            ProjectMock project2 = new ProjectMock("project2");

            var nugetPackage = new PackageName("mypackage", new SemanticVersion("1.1.0"));
            var packages = new[] { nugetPackage };

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, packages, project1, project2);
            packageInstaller.InstallPackageAction = (p) =>
            {
                cts.Cancel(); // Cancel the next one (should complete the first one)
            };

            // Act
            testSubject.InstallPackages(new ConfigurableProgressController(), cts.Token, progressEvents);

            // Verify
            packageInstaller.AssertInstalledPackages(project1, packages);
            packageInstaller.AssertNoInstalledPackages(project2);
        }

        [TestMethod]
        public void BindingWorkflow_PersistBinding()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://xxx"), "user", "pwd".ConvertToSecureString());
            var projectInfo = new ProjectInformation { Key = "ProjectKey 1" };
            var testSubject = this.CreateTestSubject(projectInfo);
            this.sonarQubeService.SetConnection(connectionInfo);
            var dte = new DTEMock();
            dte.Solution = new SolutionMock(dte, Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName, "solution.sln"));
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            var store = new ConfigurableCredentialStore();
            this.projectSystemHelper.SolutionItemsProject = dte.Solution.AddOrGetProject("Solution items folder");

            // Sanity
            store.AssertHasNoCredentials(connectionInfo.ServerUri);
            Assert.AreEqual(0, this.projectSystemHelper.SolutionItemsProject.ProjectItems.Count, "Not expecting any project items");

            // Act
            testSubject.PersistBinding(store, this.projectSystemHelper);

            // Verify
            store.AssertHasCredentials(connectionInfo.ServerUri);
            Assert.AreEqual(1, this.projectSystemHelper.SolutionItemsProject.ProjectItems.Count, "Expect configuration file to be added to the solution items folder");
        }

        #endregion

        #region Helpers

        private BindingWorkflow CreateTestSubject(ProjectInformation projectInfo = null, IRuleSetGenerationFileSystem fileSystem = null, ProjectRuleSetWriter projectWriter = null)
        {
            var useProjectInfo = projectInfo ?? new ProjectInformation();
            var slnWriter = new SolutionRuleSetWriter(useProjectInfo, fileSystem);
            var useProjectWriter = projectWriter ?? new ProjectRuleSetWriter(fileSystem);

            var controller = new ConnectSectionController(this.serviceProvider, new TransferableVisualState(), this.sonarQubeService, new ConfigurableActiveSolutionTracker(), new ConfigurableWebBrowser(), Dispatcher.CurrentDispatcher);

            return new BindingWorkflow(controller.BindCommand, useProjectInfo, slnWriter, useProjectWriter, this.projectSystemHelper);
        }

        private ConfigurablePackageInstaller PrepareInstallPackagesTest(BindingWorkflow testSubject, IEnumerable<PackageName> nugetPackages, params Project[] managedProjects)
        {
            this.projectSystemHelper.ManagedProjects = managedProjects;
            foreach (var package in nugetPackages.Select(x => new NuGetPackageInfo { Id = x.Id, Version = x.Version.ToNormalizedString() }))
            {
                testSubject.NuGetPackages.Add(package);
            }

            ConfigurablePackageInstaller packageInstaller = new ConfigurablePackageInstaller(nugetPackages);
            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(packageInstaller)));

            return packageInstaller;
        }

        private void ConfigureProfileExport(BindingWorkflow testSubject, RoslynExportProfile export, string language, RuleSetGroup group)
        {
            this.sonarQubeService.ReturnExport[language] = export;
            testSubject.LanguageToGroupMapping[language] = group;
        }

        private static void VerifyNuGetPackgesDownloaded(IEnumerable<PackageName> expectedPackages, BindingWorkflow testSubject)
        {
            var expected = expectedPackages.ToArray();
            var actual = testSubject.NuGetPackages.Select(x => new PackageName(x.Id, new SemanticVersion(x.Version))).ToArray();

            Assert.AreEqual(expected.Length, actual.Length, "Different number of packages.");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(expected[i].Equals(actual[i]), $"Packages are different at index {i}.");
            }
        }

        #endregion
    }
}
