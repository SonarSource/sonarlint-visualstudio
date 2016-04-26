//-----------------------------------------------------------------------
// <copyright file="BindingWorkflowTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            var sccFileSystem = new ConfigurableSourceControlledFileSystem();
            var ruleSerializer = new ConfigurableRuleSetSerializer(sccFileSystem);
            var solutionBinding = new ConfigurableSolutionBindingSerializer();

            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), sccFileSystem);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), ruleSerializer);
            this.serviceProvider.RegisterService(typeof(ISolutionBindingSerializer), solutionBinding);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
        }

        #region Tests
        [TestMethod]
        public void BindingWorkflow_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new BindingWorkflow(null, new ProjectInformation()));
            Exceptions.Expect<ArgumentNullException>(() => new BindingWorkflow(new ConfigurableHost(), null));
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

            var language = Language.CSharp;
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
                string.Format(CultureInfo.CurrentCulture, Strings.DownloadingQualityProfileProgressMessage, language.Name),
                string.Empty,
                Strings.QualityProfileDownloadedSuccessfulMessage);
        }

        [TestMethod]
        public void BindingWorkflow_DownloadQualityProfile_Failure()
        {
            // Setup
            BindingWorkflow testSubject = this.CreateTestSubject();
            ConfigurableProgressController controller = new ConfigurableProgressController();

            var language = Language.CSharp;
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
        public void BindingWorkflow_InstallPackages_FailureOnOneProject_Continues()
        {
            // Setup
            const string failureMessage = "Failure for project1";
            const string project1Name = "project1";
            const string project2Name = "project2";

            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var cts = new CancellationTokenSource();

            ProjectMock project1 = new ProjectMock(project1Name);
            ProjectMock project2 = new ProjectMock(project2Name);

            var nugetPackage = new PackageName("mypackage", new SemanticVersion("1.1.0"));
            var packages = new[] { nugetPackage };

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, packages, project1, project2);
            packageInstaller.InstallPackageAction = (p) =>
            {
                packageInstaller.InstallPackageAction = null;
                throw new Exception(failureMessage);
            };

            // Act
            testSubject.InstallPackages(new ConfigurableProgressController(), cts.Token, progressEvents);

            // Verify
            packageInstaller.AssertNoInstalledPackages(project1);
            packageInstaller.AssertInstalledPackages(project2, packages);
            outputWindowPane.AssertOutputStrings(string.Format(CultureInfo.CurrentCulture, Strings.FailedDuringNuGetPackageInstall, nugetPackage.Id, project1Name, failureMessage));
        }

        [TestMethod]
        public void BindingWorkflow_EmitBindingCompleteMessage()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            // Test case 1: Default state is 'true'
            Assert.IsTrue(testSubject.AllNuGetPackagesInstalled, $"Initial state of {nameof(BindingWorkflow.AllNuGetPackagesInstalled)} should be true");

            // Test case 2: All packages installed
            // Setup
            var notificationsOk = new ConfigurableProgressStepExecutionEvents();
            testSubject.AllNuGetPackagesInstalled = true;

            // Act
            testSubject.EmitBindingCompleteMessage(notificationsOk);

            // Verify
            notificationsOk.AssertProgressMessages(string.Format(CultureInfo.CurrentCulture, Strings.FinishedSolutionBindingWorkflowSuccessful));

            // Test case 3: Not all packages installed
            // Setup
            var notificationsFail = new ConfigurableProgressStepExecutionEvents();
            testSubject.AllNuGetPackagesInstalled = false;

            // Act
            testSubject.EmitBindingCompleteMessage(notificationsFail);

            // Verify
            notificationsFail.AssertProgressMessages(string.Format(CultureInfo.CurrentCulture, Strings.FinishedSolutionBindingWorkflowNotAllPackagesInstalled));
        }

        [TestMethod]
        public void BindingWorkflow_PromptSaveSolutionIfDirty()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);
            var controller = new ConfigurableProgressController();

            // Case 1: Users saves the changes
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) => VSConstants.S_OK;
            // Act
            testSubject.PromptSaveSolutionIfDirty(controller, CancellationToken.None);
            // Verify
            this.outputWindowPane.AssertOutputStrings(0);
            controller.AssertNumberOfAbortRequests(0);

            // Case 2: Users cancels the save
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) => VSConstants.S_FALSE;
            // Act
            testSubject.PromptSaveSolutionIfDirty(controller, CancellationToken.None);
            // Verify
            this.outputWindowPane.AssertOutputStrings(Strings.SolutionSaveCancelledBindAborted);
            controller.AssertNumberOfAbortRequests(1);
        }

        [TestMethod]
        public void BindingWorkflow_SilentSaveSolutionIfDirty()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var solution = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solution);
            solution.SaveSolutionElementAction = (options, hierarchy, docCookie) => VSConstants.S_OK;

            // Act
            testSubject.SilentSaveSolutionIfDirty();

            // Verify
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void BindingWorkflow_GetBindingLanguages_ReturnsDistinctLanguagesForProjects()
        {
            // Setup
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
            testSubject.BindingProjects.AddRange(projects);

            var expectedLanguages = new[] { Language.CSharp, Language.VBNET };

            // Act
            var actualLanguages = testSubject.GetBindingLanguages();

            // Verify
            CollectionAssert.AreEquivalent(expectedLanguages, actualLanguages.ToArray(), "Unexpected languages for binding projects");
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_AddsMatchingProjectsToBinding()
        {
            // Setup
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            var csProject1 = new ProjectMock("cs1.csproj");
            var csProject2 = new ProjectMock("cs2.csproj");
            csProject1.SetCSProjectKind();
            csProject2.SetCSProjectKind();

            var matchingProjects = new[] { csProject1, csProject2 };
            this.projectSystemHelper.FilteredProjects = matchingProjects;

            var testSubject = this.CreateTestSubject();

            // Act
            testSubject.DiscoverProjects(controller, progressEvents);

            // Verify
            CollectionAssert.AreEqual(matchingProjects, testSubject.BindingProjects.ToArray(), "Unexpected projects selected for binding");
            progressEvents.AssertProgressMessages(Strings.DiscoveringSolutionProjectsProgressMessage);
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_OutputsExcludedProjects()
        {
            // Setup
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            List<Project> projects = new List<Project>();
            for (int i = 0; i < 4; i++)
            {
                projects.Add(new ProjectMock($"cs{i}.csproj"));
            }

            this.projectSystemHelper.FilteredProjects = projects.Take(2);
            this.projectSystemHelper.Projects = projects;

            var testSubject = this.CreateTestSubject();

            // Act
            testSubject.DiscoverProjects(controller, progressEvents);

            // Verify
            this.outputWindowPane.AssertOutputStrings(1);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(0, new[] { projects[2].UniqueName, projects[3].UniqueName });
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_NoMatchingProjects_AbortsWorkflow()
        {
            // Setup
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            this.projectSystemHelper.FilteredProjects = null;

            var testSubject = this.CreateTestSubject();

            // Act
            testSubject.DiscoverProjects(controller, progressEvents);

            // Verify
            Assert.AreEqual(0, testSubject.BindingProjects.Count, "Expected no projects selected for binding");
            progressEvents.AssertProgressMessages(Strings.DiscoveringSolutionProjectsProgressMessage);
            this.outputWindowPane.AssertOutputStrings(Strings.NoProjectsApplicableForBinding);
            controller.AssertNumberOfAbortRequests(1);
        }

        #endregion

        #region Helpers

        private BindingWorkflow CreateTestSubject(ProjectInformation projectInfo = null)
        {
            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.sonarQubeService.SetConnection(new Uri("http://connected"));
            host.SonarQubeService = this.sonarQubeService;
            var useProjectInfo = projectInfo ?? new ProjectInformation { Key = "key" };

            return new BindingWorkflow(host, useProjectInfo);
        }

        private ConfigurablePackageInstaller PrepareInstallPackagesTest(BindingWorkflow testSubject, IEnumerable<PackageName> nugetPackages, params Project[] projects)
        {
            testSubject.BindingProjects.Clear();
            testSubject.BindingProjects.AddRange(projects);

            foreach (var package in nugetPackages.Select(x => new NuGetPackageInfo { Id = x.Id, Version = x.Version.ToNormalizedString() }))
            {
                testSubject.NuGetPackages.Add(package);
            }

            ConfigurablePackageInstaller packageInstaller = new ConfigurablePackageInstaller(nugetPackages);
            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(packageInstaller)));

            return packageInstaller;
        }

        private void ConfigureProfileExport(BindingWorkflow testSubject, RoslynExportProfile export, Language language, RuleSetGroup group)
        {
            this.sonarQubeService.ReturnExport[language.ServerKey] = export;
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
