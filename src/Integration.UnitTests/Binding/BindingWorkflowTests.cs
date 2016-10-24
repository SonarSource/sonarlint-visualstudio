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
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BindingWorkflowTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

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
            var validConnection = new ConnectionInformation(new Uri("http://server"));
            var validProjectInfo = new ProjectInformation();
            var validHost = new ConfigurableHost();

            Exceptions.Expect<ArgumentNullException>(() => new BindingWorkflow(null, validConnection, validProjectInfo));
            Exceptions.Expect<ArgumentNullException>(() => new BindingWorkflow(validHost, null, validProjectInfo));
            Exceptions.Expect<ArgumentNullException>(() => new BindingWorkflow(validHost, validConnection, null));
        }

        [TestMethod]
        public void BindingWorkflow_DownloadQualityProfile_Success()
        {
            // Setup
            const string QualityProfileName = "SQQualityProfileName";
            const string SonarQubeProjectName = "SQProjectName";
            var projectInfo = new ProjectInformation { Key = "key", Name = SonarQubeProjectName };
            BindingWorkflow testSubject = this.CreateTestSubject(projectInfo);
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "Key1", "Key2" });
            var expectedRuleSet = new RuleSet(ruleSet)
            {
                NonLocalizedDisplayName = string.Format(Strings.SonarQubeRuleSetNameFormat, SonarQubeProjectName, QualityProfileName),
                NonLocalizedDescription = "\r\nhttp://connected/profiles/show?key="
            };
            var nugetPackages = new[] { new PackageName("myPackageId", new SemanticVersion("1.0.0")) };
            var additionalFiles = new[] { new AdditionalFile { FileName = "abc.xml", Content = new byte[] { 1, 2, 3 } } };
            RoslynExportProfile export = RoslynExportProfileHelper.CreateExport(ruleSet, nugetPackages, additionalFiles);

            var language = Language.VBNET;
            QualityProfile profile = this.ConfigureProfileExport(export, language);
            profile.Name = QualityProfileName;

            // Act
            testSubject.DownloadQualityProfile(controller, CancellationToken.None, notifications, new[] { language });

            // Verify
            RuleSetAssert.AreEqual(expectedRuleSet, testSubject.Rulesets[language], "Unexpected rule set");
            Assert.AreSame(profile, testSubject.QualityProfiles[language]);
            VerifyNuGetPackgesDownloaded(nugetPackages, testSubject);
            controller.AssertNumberOfAbortRequests(0);
            notifications.AssertProgress(
                0.0,
                1.0);
            notifications.AssertProgressMessages(Strings.DownloadingQualityProfileProgressMessage, string.Empty);

            this.outputWindowPane.AssertOutputStrings(1);
            var expectedOutput = "   Successfully downloaded quality profile, Name: \"" + QualityProfileName + "\", Key: \"\", Language: \"VB.NET\"";
            this.outputWindowPane.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public void BindingWorkflow_DownloadQualityProfile_Failure()
        {
            // Setup
            BindingWorkflow testSubject = this.CreateTestSubject();
            ConfigurableProgressController controller = new ConfigurableProgressController();
            var notifications = new ConfigurableProgressStepExecutionEvents();

            var language = Language.CSharp;
            this.ConfigureProfileExport(null, language);

            // Act
            testSubject.DownloadQualityProfile(controller, CancellationToken.None, notifications, new[] { language });

            // Verify
            Assert.IsFalse(testSubject.Rulesets.ContainsKey(Language.VBNET), "Not expecting any rules for this language");
            Assert.IsFalse(testSubject.Rulesets.ContainsKey(language), "Not expecting any rules");
            controller.AssertNumberOfAbortRequests(1);

            notifications.AssertProgressMessages(Strings.DownloadingQualityProfileProgressMessage);

            this.outputWindowPane.AssertOutputStrings(1);
            var expectedOutput = "   Failed to download quality profile, Name: \"\", Key: \"\", Language: \"C#\"";
            this.outputWindowPane.AssertOutputStrings(expectedOutput);
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

        private void BindingWorkflow_DiscoverProjects_GenericPart(ConfigurableProgressController controller, ConfigurableProgressStepExecutionEvents progressEvents, int numberOfProjectsToCreate, int numberOfProjectsToInclude)
        {
            // Setup
            List<Project> projects = new List<Project>();
            for (int i = 0; i < numberOfProjectsToCreate; i++)
            {
                projects.Add(new ProjectMock($"cs{i}.csproj"));
            }

            this.projectSystemHelper.FilteredProjects = projects.Take(numberOfProjectsToInclude);
            this.projectSystemHelper.Projects = projects;

            var testSubject = this.CreateTestSubject();

            // Act
            testSubject.DiscoverProjects(controller, progressEvents);

            // Verify
            Assert.AreEqual(numberOfProjectsToInclude, testSubject.BindingProjects.Count, "Expected " + numberOfProjectsToInclude + " project(s) selected for binding");
            progressEvents.AssertProgressMessages(Strings.DiscoveringSolutionProjectsProgressMessage);
            this.outputWindowPane.AssertOutputStrings(1);

            // Returns expected output message
            var expectedOutput = new StringBuilder("   Included projects:\r\n");
            if (numberOfProjectsToInclude > 0)
            {
                this.projectSystemHelper.FilteredProjects.ToList().ForEach(p => expectedOutput.AppendFormat("   * {0}\r\n", p.Name));
            }
            else
            {
                expectedOutput.Append("   * None (for selected SonarQube project's quality profile)\r\n");
            }
            expectedOutput.Append("   Excluded projects:\r\n");
            if (numberOfProjectsToCreate - numberOfProjectsToInclude > 0)
            {
                this.projectSystemHelper.Projects.Except(this.projectSystemHelper.FilteredProjects)
                                                 .ToList()
                                                 .ForEach(p => expectedOutput.AppendFormat("   * {0}\r\n", p.Name));
            }
            else
            {
                expectedOutput.Append("   * None (for selected SonarQube project's quality profile)\r\n");
            }
            expectedOutput.Append("   You can change the exclusion options via the SonarLint project-level context menu i.e. Solution Explorer -> Select project(s)");

            this.outputWindowPane.AssertOutputStrings(expectedOutput.ToString());
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_OutputsIncludedProjects()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            // Act & Common Assert
            BindingWorkflow_DiscoverProjects_GenericPart(controller, progressEvents, 2, 2);
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_OutputsExcludedProjects()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            // Act & Common Assert
            BindingWorkflow_DiscoverProjects_GenericPart(controller, progressEvents, 2, 0);
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_OutputsIncludedAndExcludedProjects()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            // Act & Common Assert
            BindingWorkflow_DiscoverProjects_GenericPart(controller, progressEvents, 4, 2);
        }

        [TestMethod]
        public void BindingWorkflow_DiscoverProjects_NoMatchingProjects_AbortsWorkflow()
        {
            // Arrange
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            // Act & Common Assert
            BindingWorkflow_DiscoverProjects_GenericPart(controller, progressEvents, 0, 0);

            // Assert
            controller.AssertNumberOfAbortRequests(1);
        }

        #endregion

        #region Helpers

        private BindingWorkflow CreateTestSubject(ProjectInformation projectInfo = null)
        {
            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            ConnectionInformation connected = new ConnectionInformation(new Uri("http://connected"));
            host.SonarQubeService = this.sonarQubeService;
            var useProjectInfo = projectInfo ?? new ProjectInformation { Key = "key" };
            return new BindingWorkflow(host, connected, useProjectInfo);
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

        private QualityProfile ConfigureProfileExport(RoslynExportProfile export, Language language)
        {
            var profile = new QualityProfile { Language = SonarQubeServiceWrapper.GetServerLanguageKey(language) };
            this.sonarQubeService.ReturnProfile[language] = profile;
            this.sonarQubeService.ReturnExport[profile] = export;

            return profile;
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
