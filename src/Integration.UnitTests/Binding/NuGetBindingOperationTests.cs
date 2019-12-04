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
using System.Linq;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Messages;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class NuGetBindingOperationTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            serviceProvider = new ConfigurableServiceProvider();
            logger = new TestLogger();
        }

        #region Tests

        [TestMethod]
        public void InstallPackages_WhenAllProjectsAreCSharp_Succeed()
        {
            InstallPackages_Succeed(ProjectSystemHelper.CSharpProjectKind, Language.CSharp);
        }

        [TestMethod]
        public void InstallPackages_WhenAllProjectsAreVbNet_Succeed()
        {
            InstallPackages_Succeed(ProjectSystemHelper.VbProjectKind, Language.VBNET);
        }

        private void InstallPackages_Succeed(string projectKind, Language language)
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(progressEvents);

            ProjectMock project1 = new ProjectMock("project1") { ProjectKind = projectKind };
            ProjectMock project2 = new ProjectMock("project2") { ProjectKind = projectKind };
            var projectsToBind = new HashSet<Project> { project1, project2 };

            var nugetPackage = new PackageName("mypackage", new SemanticVersion("1.1.0"));
            var packages = new Dictionary<Language, IEnumerable<PackageName>>();
            packages.Add(language, new[] { nugetPackage });

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, packages);

            // Act
            testSubject.InstallPackages(projectsToBind, progressAdapter, CancellationToken.None);

            // Assert
            packageInstaller.AssertInstalledPackages(project1, new[] { nugetPackage });
            packageInstaller.AssertInstalledPackages(project2, new[] { nugetPackage });
            this.logger.AssertOutputStrings(4);
            this.logger.AssertOutputStrings(
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.EnsuringNugetPackagesProgressMessage, nugetPackage.Id, ((Project)project1).Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.SuccessfullyInstalledNugetPackageForProject, nugetPackage.Id, ((Project)project1).Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.EnsuringNugetPackagesProgressMessage, nugetPackage.Id, ((Project)project2).Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.SuccessfullyInstalledNugetPackageForProject, nugetPackage.Id, ((Project)project2).Name))
                );
            progressEvents.AssertProgress(.5, 1.0);
        }

        [TestMethod]
        public void InstallPackages_MoreNugetPackagesThanLanguageCount()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(progressEvents);

            var project1 = new ProjectMock("project1") { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            var projectsToBind = new HashSet<Project> { project1 };

            var nugetPackage1 = new PackageName("mypackage1", new SemanticVersion("1.1.0"));
            var nugetPackage2 = new PackageName("mypackage2", new SemanticVersion("1.1.1"));
            var nugetPackages = new[] { nugetPackage1, nugetPackage2 };
            var packages = new Dictionary<Language, IEnumerable<PackageName>>();
            packages.Add(Language.CSharp, nugetPackages);

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, packages);

            // Act
            testSubject.InstallPackages(projectsToBind, progressAdapter, CancellationToken.None);

            // Assert
            packageInstaller.AssertInstalledPackages(project1, nugetPackages);
            this.logger.AssertOutputStrings(4);
            this.logger.AssertOutputStrings(
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.EnsuringNugetPackagesProgressMessage, nugetPackages[0].Id, ((Project)project1).Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.SuccessfullyInstalledNugetPackageForProject, nugetPackages[0].Id, ((Project)project1).Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.EnsuringNugetPackagesProgressMessage, nugetPackages[1].Id, ((Project)project1).Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.SuccessfullyInstalledNugetPackageForProject, nugetPackages[1].Id, ((Project)project1).Name))
                );
            progressEvents.AssertProgress(.5, 1.0);
        }

        [TestMethod]
        public void InstallPackages_Cancellation()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(progressEvents);
            var cts = new CancellationTokenSource();

            var project1 = new ProjectMock("project1") { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            var project2 = new ProjectMock("project2") { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            var projectsToBind = new HashSet<Project> { project1, project2 };

            var nugetPackage = new PackageName("mypackage", new SemanticVersion("1.1.0"));
            var packages = new[] { nugetPackage };
            var nugetPackagesByLanguage = new Dictionary<Language, IEnumerable<PackageName>>();
            nugetPackagesByLanguage.Add(Language.CSharp, packages);

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, nugetPackagesByLanguage);
            packageInstaller.InstallPackageAction = (p) =>
            {
                cts.Cancel(); // Cancel the next one (should complete the first one)
            };

            // Acts
            testSubject.InstallPackages(projectsToBind, progressAdapter, cts.Token);

            // Assert
            packageInstaller.AssertInstalledPackages(project1, packages);
            packageInstaller.AssertNoInstalledPackages(project2);

            progressEvents.AssertProgress(.5);
        }

        [TestMethod]
        public void InstallPackages_FailureOnOneProject_Continues()
        {
            // Arrange
            const string failureMessage = "Failure for project1";
            const string project1Name = "project1";
            const string project2Name = "project2";

            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(progressEvents);
            var cts = new CancellationTokenSource();

            ProjectMock project1 = new ProjectMock(project1Name) { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            ProjectMock project2 = new ProjectMock(project2Name) { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            var projectsToBind = new HashSet<Project> { project1, project2 };

            var nugetPackage = new PackageName("mypackage", new SemanticVersion("1.1.0"));
            var packages = new[] { nugetPackage };
            var nugetPackages = new Dictionary<Language, IEnumerable<PackageName>>();
            nugetPackages.Add(Language.CSharp, packages);

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, nugetPackages);
            packageInstaller.InstallPackageAction = (p) =>
            {
                packageInstaller.InstallPackageAction = null;
                throw new Exception(failureMessage);
            };

            // Act
            testSubject.InstallPackages(projectsToBind, progressAdapter, cts.Token);

            // Assert
            packageInstaller.AssertNoInstalledPackages(project1);
            packageInstaller.AssertInstalledPackages(project2, packages);
            this.logger.AssertOutputStrings(4);
            this.logger.AssertOutputStrings(
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.EnsuringNugetPackagesProgressMessage, nugetPackage.Id, project1Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.FailedDuringNuGetPackageInstall, nugetPackage.Id, project1Name, failureMessage)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.EnsuringNugetPackagesProgressMessage, nugetPackage.Id, project2Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.SuccessfullyInstalledNugetPackageForProject, nugetPackage.Id, project2Name))
                );

            progressEvents.AssertProgress(.5, 1.0);
        }

        [TestMethod]
        public void InstallPackages_WhenProjectLanguageDoesNotExist_PrintMessageAndContinue()
        {
            // Arrange
            const string project1Name = "project1";
            const string project2Name = "project2";

            var testSubject = this.CreateTestSubject();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(progressEvents);

            ProjectMock project1 = new ProjectMock(project1Name); // No project kind so no nuget package will be installed
            ProjectMock project2 = new ProjectMock(project2Name) { ProjectKind = ProjectSystemHelper.CSharpProjectKind };
            var projectsToBind = new HashSet<Project> { project1, project2 };

            var nugetPackage = new PackageName("mypackage", new SemanticVersion("1.1.0"));
            var packages = new[] { nugetPackage };
            var nugetPackages = new Dictionary<Language, IEnumerable<PackageName>>();
            nugetPackages.Add(Language.CSharp, packages);

            ConfigurablePackageInstaller packageInstaller = this.PrepareInstallPackagesTest(testSubject, nugetPackages);

            // Act
            testSubject.InstallPackages(projectsToBind, progressAdapter, CancellationToken.None);

            // Assert
            packageInstaller.AssertNoInstalledPackages(project1);
            packageInstaller.AssertInstalledPackages(project2, packages);
            this.logger.AssertOutputStrings(3);
            this.logger.AssertOutputStrings(
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.BindingProjectLanguageNotMatchingAnyQualityProfileLanguage, project1Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.EnsuringNugetPackagesProgressMessage, nugetPackage.Id, project2Name)),
                string.Format(Strings.SubTextPaddingFormat, string.Format(Strings.SuccessfullyInstalledNugetPackageForProject, nugetPackage.Id, project2Name))
                );
        }

        #endregion

        #region Helpers

        private NuGetBindingOperation CreateTestSubject()
        {
            var testSubject = new NuGetBindingOperation(this.serviceProvider, this.logger);
            return testSubject;
        }

        private ConfigurablePackageInstaller PrepareInstallPackagesTest(NuGetBindingOperation testSubject, Dictionary<Language, IEnumerable<PackageName>> nugetPackagesByLanguage)
        {
            var exportResponse = new RoslynExportProfileResponse
            {
                Deployment = new DeploymentResponse
                {
                    NuGetPackages = new List<NuGetPackageInfoResponse>()
                }
            };

            foreach (var nugetPackagesForLanguage in nugetPackagesByLanguage)
            {
                testSubject.NuGetPackages.Add(nugetPackagesForLanguage.Key,
                    nugetPackagesForLanguage.Value.Select(x => new NuGetPackageInfoResponse { Id = x.Id, Version = x.Version.ToNormalizedString() }).ToList());
            }


            ConfigurablePackageInstaller packageInstaller = new ConfigurablePackageInstaller(nugetPackagesByLanguage.Values.SelectMany(x => x));
            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(packageInstaller)));

            return packageInstaller;
        }

        #endregion
    }
}
