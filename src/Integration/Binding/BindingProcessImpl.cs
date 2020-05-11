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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class BindingProcessImpl : IBindingProcess
    {
        private readonly IHost host;
        private readonly BindCommandArgs bindingArgs;
        private readonly IProjectSystemHelper projectSystem;
        private readonly ISolutionBindingOperation solutionBindingOperation;
        private readonly IUnboundProjectFinder unboundProjectFinder;
        private readonly IBindingConfigProvider bindingConfigProvider;
        private readonly SonarLintMode bindingMode;

        internal /*for testing*/ INuGetBindingOperation NuGetBindingOperation { get; }

        public BindingProcessImpl(IHost host,
            BindCommandArgs bindingArgs,
            ISolutionBindingOperation solutionBindingOperation,
            INuGetBindingOperation nugetBindingOperation,
            IUnboundProjectFinder unboundProjectFinder,
            IBindingConfigProvider bindingConfigProvider,
            SonarLintMode bindingMode,
            bool isFirstBinding = false)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.bindingArgs = bindingArgs ?? throw new ArgumentNullException(nameof(bindingArgs));
            this.solutionBindingOperation = solutionBindingOperation ?? throw new ArgumentNullException(nameof(solutionBindingOperation));
            this.NuGetBindingOperation = nugetBindingOperation ?? throw new ArgumentNullException(nameof(nugetBindingOperation));
            this.unboundProjectFinder = unboundProjectFinder ?? throw new ArgumentNullException(nameof(unboundProjectFinder));
            this.bindingConfigProvider = bindingConfigProvider ?? throw new ArgumentNullException(nameof(bindingConfigProvider));
            this.bindingMode = bindingMode;

            Debug.Assert(bindingArgs.ProjectKey != null);
            Debug.Assert(bindingArgs.ProjectName != null);
            Debug.Assert(bindingArgs.Connection != null);

            this.projectSystem = this.host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.InternalState = new BindingProcessState(isFirstBinding);
        }

        // This property should not be used by product code
        internal /* for testing */ BindingProcessState InternalState { get;  }

        #region IBindingTemplate methods

        public bool PromptSaveSolutionIfDirty()
        {
            var result = VsShellUtils.SaveSolution(this.host, silent: false);
            if (!result)
            {
                this.host.Logger.WriteLine(Strings.SolutionSaveCancelledBindAborted);
            }
            return result;
        }

        public bool DiscoverProjects()
        {
            var patternFilteredProjects = this.projectSystem.GetFilteredSolutionProjects();
            var pluginAndPatternFilteredProjects =
                patternFilteredProjects.Where(p => this.host.SupportedPluginLanguages
                    .Any(l => ProjectToLanguageMapper.GetAllBindingLanguagesForProject(p).Contains(l)));

            this.InternalState.BindingProjects.UnionWith(pluginAndPatternFilteredProjects);
            this.InformAboutFilteredOutProjects();

            return this.InternalState.BindingProjects.Any();
        }

        public async Task<bool> DownloadQualityProfileAsync(IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken)
        {
            var languageList = this.GetBindingLanguages();

            var languageCount = languageList.Count();
            int currentLanguage = 0;
            progress?.Report(new FixedStepsProgress(Strings.DownloadingQualityProfileProgressMessage, currentLanguage, languageCount));

            foreach (var language in languageList)
            {
                var serverLanguage = language.ServerLanguage;

                // Download the quality profile for each language
                var qualityProfileInfo = await Core.WebServiceHelper.SafeServiceCallAsync(() =>
                    this.host.SonarQubeService.GetQualityProfileAsync(
                        this.bindingArgs.ProjectKey, this.bindingArgs.Connection.Organization?.Key, serverLanguage, cancellationToken),
                    this.host.Logger);
                if (qualityProfileInfo == null)
                {
                    this.host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                       string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name)));
                    return false;
                }
                this.InternalState.QualityProfiles[language] = qualityProfileInfo;

                var bindingConfiguration = QueueWriteBindingInformation();

                // Create the binding configuration for the language
                var bindingConfig = await this.bindingConfigProvider.GetConfigurationAsync(qualityProfileInfo, language, bindingConfiguration, cancellationToken);
                if (bindingConfig == null)
                {
                    this.host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                        string.Format(Strings.FailedToCreateBindingConfigForLanguage, language.Name)));
                    return false;
                }

                this.InternalState.BindingConfigs[language] = bindingConfig;

                currentLanguage++;
                progress?.Report(new FixedStepsProgress(string.Empty, currentLanguage, languageCount));

                this.host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadSuccessfulMessageFormat, qualityProfileInfo.Name, qualityProfileInfo.Key, language.Name)));
            }

            return true;
        }

        /// <summary>
        /// Will add/edit the binding information for next time usage
        /// </summary>
        private BindingConfiguration QueueWriteBindingInformation()
        {
            Debug.Assert(InternalState.QualityProfiles != null, "Initialize was expected to be called first");

            var configurationPersister = host.GetService<IConfigurationPersister>();
            configurationPersister.AssertLocalServiceIsNotNull();

            BasicAuthCredentials credentials = bindingArgs.Connection.UserName == null ? null : new BasicAuthCredentials(bindingArgs.Connection.UserName, bindingArgs.Connection.Password);

            Dictionary<Language, ApplicableQualityProfile> map = new Dictionary<Language, ApplicableQualityProfile>();

            foreach (var keyValue in InternalState.QualityProfiles)
            {
                map[keyValue.Key] = new ApplicableQualityProfile
                {
                    ProfileKey = keyValue.Value.Key,
                    ProfileTimestamp = keyValue.Value.TimeStamp
                };
            }

            var bound = new BoundSonarQubeProject(bindingArgs.Connection.ServerUri,
                bindingArgs.ProjectKey,
                bindingArgs.ProjectName,
                credentials,
                bindingArgs.Connection.Organization);

            bound.Profiles = map;

            return configurationPersister.Persist(bound, bindingMode);
        }

        public void PrepareToInstallPackages()
        {
            this.NuGetBindingOperation.PrepareOnUIThread();
        }

        public void InstallPackages(IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken)
        {
            this.InternalState.BindingOperationSucceeded = this.NuGetBindingOperation.InstallPackages(this.InternalState.BindingProjects, progress, cancellationToken);
        }

        public void InitializeSolutionBindingOnUIThread()
        {
            this.solutionBindingOperation.RegisterKnownConfigFiles(this.InternalState.BindingConfigs);

            var projectsToUpdate = GetProjectsForRulesetBinding(this.InternalState.IsFirstBinding, this.InternalState.BindingProjects.ToArray(),
                this.unboundProjectFinder, this.host.Logger);

            this.solutionBindingOperation.Initialize(projectsToUpdate);
        }

        public void PrepareSolutionBinding(CancellationToken cancellationToken)
        {
            this.solutionBindingOperation.Prepare(cancellationToken);
        }

        public bool FinishSolutionBindingOnUIThread()
        {
            return this.solutionBindingOperation.CommitSolutionBinding();
        }

        public void SilentSaveSolutionIfDirty()
        {
            bool saved = VsShellUtils.SaveSolution(this.host, silent: true);
            Debug.Assert(saved, "Should not be cancellable");
        }

        public bool BindOperationSucceeded => InternalState.BindingOperationSucceeded;

        #endregion

        #region Private methods

        private void InformAboutFilteredOutProjects()
        {
            var includedProjects = this.InternalState.BindingProjects.ToList();
            var excludedProjects = this.projectSystem.GetSolutionProjects().Except(this.InternalState.BindingProjects).ToList();

            var output = new StringBuilder();

            output.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionIncludedProjectsHeader).AppendLine();
            if (includedProjects.Count == 0)
            {
                var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, Strings.NoProjectsApplicableForBinding);
                output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
            }
            else
            {
                includedProjects.ForEach(
                    p =>
                    {
                        var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, p.UniqueName);
                        output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
                    });
            }

            output.AppendFormat(Strings.SubTextPaddingFormat, Strings.DiscoveringSolutionExcludedProjectsHeader).AppendLine();
            if (excludedProjects.Count == 0)
            {
                var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, Strings.NoProjectsExcludedFromBinding);
                output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
            }
            else
            {
                excludedProjects.ForEach(
                    p =>
                    {
                        var message = string.Format(Strings.DiscoveredIncludedOrExcludedProjectFormat, p.UniqueName);
                        output.AppendFormat(Strings.SubTextPaddingFormat, message).AppendLine();
                    });
            }
            output.AppendFormat(Strings.SubTextPaddingFormat, Strings.FilteredOutProjectFromBindingEnding);

            this.host.Logger.WriteLine(output.ToString());
        }

        internal /* for testing */ IEnumerable<Language> GetBindingLanguages()
        {
            var languageList = this.InternalState.BindingProjects.SelectMany(ProjectToLanguageMapper.GetAllBindingLanguagesForProject)
                                       .Distinct()
                                       .Where(this.host.SupportedPluginLanguages.Contains)
                                       .ToList();
            return languageList;
        }

        internal /* for testing purposes */ static Project[] GetProjectsForRulesetBinding(bool isFirstBinding,
            Project[] allSupportedProjects,
            IUnboundProjectFinder unboundProjectFinder,
            ILogger logger)
        {
            // If we are already bound we don't need to update/create rulesets in projects
            // that already have the ruleset information configured
            var projectsToUpdate = allSupportedProjects;

            if (isFirstBinding)
            {
                logger.WriteLine(Strings.Bind_Ruleset_InitialBinding);
            }
            else
            {
                var unboundProjects = unboundProjectFinder.GetUnboundProjects()?.ToArray() ?? new Project[] { };
                projectsToUpdate = projectsToUpdate.Intersect(unboundProjects).ToArray();

                var upToDateProjects = allSupportedProjects.Except(unboundProjects);
                if (upToDateProjects.Any())
                {
                    logger.WriteLine(Strings.Bind_Ruleset_SomeProjectsDoNotNeedToBeUpdated);
                    var projectList = string.Join(", ", upToDateProjects.Select(p => p.Name));
                    logger.WriteLine($"  {projectList}");
                }
                else
                {
                    logger.WriteLine(Strings.Bind_Ruleset_AllProjectsNeedToBeUpdated);
                }
            }
            return projectsToUpdate;
        }

        #endregion

        #region Workflow state

        internal class BindingProcessState
        {
            public BindingProcessState(bool isFirstBinding)
            {
                this.IsFirstBinding = isFirstBinding;
            }

            public bool IsFirstBinding { get; }

            public ISet<Project> BindingProjects
            {
                get;
            } = new HashSet<Project>();

            public Dictionary<Language, IBindingConfig> BindingConfigs
            {
                get;
            } = new Dictionary<Language, IBindingConfig>();

            public Dictionary<Language, SonarQubeQualityProfile> QualityProfiles
            {
                get;
            } = new Dictionary<Language, SonarQubeQualityProfile>();

            public bool BindingOperationSucceeded
            {
                get;
                set;
            } = true;
        }

        #endregion
    }
}
