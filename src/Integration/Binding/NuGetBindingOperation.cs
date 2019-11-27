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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Messages;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class NuGetBindingOperation : INuGetBindingOperation
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;

        public NuGetBindingOperation(IServiceProvider serviceProvider, ILogger logger)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        internal /*for testing*/ Dictionary<Language, List<NuGetPackageInfoResponse>> NuGetPackages
        {
            get;
        } = new Dictionary<Language, List<NuGetPackageInfoResponse>>();


        public void PrepareOnUIThread()
        {
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected step to be run on the UI thread");
            NuGetHelper.LoadService(this.serviceProvider); /*The service needs to be loaded on UI thread*/
        }

        /// <summary>
        /// Will install the NuGet packages for the current managed projects.
        /// The packages that will be installed will be based on the information from <see cref="Analyzer.GetRequiredNuGetPackages"/>
        /// and is specific to the <see cref="RuleSet"/>.
        /// </summary>
        public bool InstallPackages(ISet<Project> projectsToBind, IProgressStepExecutionEvents notificationEvents, CancellationToken token)
        {
            if (this.NuGetPackages.Count == 0)
            {
                return true;
            }

            Debug.Assert(this.NuGetPackages.Count == this.NuGetPackages.Distinct().Count(), "Duplicate NuGet packages specified");

            if (projectsToBind == null || projectsToBind.Count == 0)
            {
                Debug.Fail("Not expected to be called when there are no projects");
                return true;
            }

            var projectNugets = projectsToBind
                .SelectMany(bindingProject =>
                {
                    var projectLanguage = Language.ForProject(bindingProject);

                    List<NuGetPackageInfoResponse> nugetPackages;
                    if (!this.NuGetPackages.TryGetValue(projectLanguage, out nugetPackages))
                    {
                        var message = string.Format(Strings.BindingProjectLanguageNotMatchingAnyQualityProfileLanguage, bindingProject.Name);
                        this.logger.WriteLine(Strings.SubTextPaddingFormat, message);
                        nugetPackages = new List<NuGetPackageInfoResponse>();
                    }

                    return nugetPackages.Select(nugetPackage => new { Project = bindingProject, NugetPackage = nugetPackage });
                })
                .ToArray();

            bool overallSuccess = true;

            DeterminateStepProgressNotifier progressNotifier = new DeterminateStepProgressNotifier(notificationEvents, projectNugets.Length);
            foreach (var projectNuget in projectNugets)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                string message = string.Format(CultureInfo.CurrentCulture, Strings.EnsuringNugetPackagesProgressMessage, projectNuget.NugetPackage.Id, projectNuget.Project.Name);
                this.logger.WriteLine(Strings.SubTextPaddingFormat, message);

                var isNugetInstallSuccessful = NuGetHelper.TryInstallPackage(this.serviceProvider, this.logger, projectNuget.Project, projectNuget.NugetPackage.Id, projectNuget.NugetPackage.Version);

                if (isNugetInstallSuccessful) // NuGetHelper.TryInstallPackage already displayed the error message so only take care of the success message
                {
                    message = string.Format(CultureInfo.CurrentCulture, Strings.SuccessfullyInstalledNugetPackageForProject, projectNuget.NugetPackage.Id, projectNuget.Project.Name);
                    this.logger.WriteLine(Strings.SubTextPaddingFormat, message);
                }

                // TODO: SVS-33 (https://jira.sonarsource.com/browse/SVS-33) Trigger a Team Explorer warning notification to investigate the partial binding in the output window.
                overallSuccess &= isNugetInstallSuccessful;

                progressNotifier.NotifyIncrementedProgress(string.Empty);
            }
            return overallSuccess;
        }

        public bool ProcessExport(Language language, RoslynExportProfileResponse exportProfileResponse)
        {
            if (exportProfileResponse.Deployment.NuGetPackages.Count == 0)
            {
                this.logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.NoNuGetPackageForQualityProfile, language.Name)));
                return false;
            }

            this.NuGetPackages.Add(language, exportProfileResponse.Deployment.NuGetPackages);
            return true;
        }
    }
}
