/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
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
        private readonly IBindingConfigProvider bindingConfigProvider;
        private readonly SonarLintMode bindingMode;
        private readonly IExclusionSettingsStorage exclusionSettingsStorage;

        public BindingProcessImpl(IHost host,
            BindCommandArgs bindingArgs,
            ISolutionBindingOperation solutionBindingOperation,
            IBindingConfigProvider bindingConfigProvider,
            SonarLintMode bindingMode,
            IExclusionSettingsStorage exclusionSettingsStorage,
            bool isFirstBinding = false)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.bindingArgs = bindingArgs ?? throw new ArgumentNullException(nameof(bindingArgs));
            this.solutionBindingOperation = solutionBindingOperation ?? throw new ArgumentNullException(nameof(solutionBindingOperation));
            this.bindingConfigProvider = bindingConfigProvider ?? throw new ArgumentNullException(nameof(bindingConfigProvider));
            this.exclusionSettingsStorage = exclusionSettingsStorage ?? throw new ArgumentNullException(nameof(exclusionSettingsStorage));
            this.bindingMode = bindingMode;

            Debug.Assert(bindingArgs.ProjectKey != null);
            Debug.Assert(bindingArgs.ProjectName != null);
            Debug.Assert(bindingArgs.Connection != null);

            this.projectSystem = this.host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.InternalState = new BindingProcessState(isFirstBinding);
        }

        // This property should not be used by product code outside this class
        internal /* for testing */ BindingProcessState InternalState { get;  }

        #region IBindingTemplate methods

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

        public async Task<bool> SaveServerExclusionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var exclusions = await host.SonarQubeService.GetServerExclusions(bindingArgs.ProjectKey, cancellationToken);
                exclusionSettingsStorage.SaveSettings(exclusions);
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                host.Logger.WriteLine(string.Format(Strings.SaveExclusionsFailed, ex.Message));
                return false;
            }
            return true;
        }

        public void InitializeSolutionBindingOnBackgroundThread()
        {
            this.solutionBindingOperation.RegisterKnownConfigFiles(this.InternalState.BindingConfigs);

            this.solutionBindingOperation.Initialize();
        }

        public void PrepareSolutionBinding(CancellationToken cancellationToken)
        {
            this.solutionBindingOperation.Prepare(cancellationToken);
        }

        public bool FinishSolutionBindingOnUIThread()
        {
            return this.solutionBindingOperation.CommitSolutionBinding();
        }

        public bool BindOperationSucceeded => InternalState.BindingOperationSucceeded;

        #endregion

        #region Private methods

        internal /* for testing */ IEnumerable<Language> GetBindingLanguages()
            => host.SupportedPluginLanguages;

        #endregion

            #region Workflow state

        internal class BindingProcessState
        {
            public BindingProcessState(bool isFirstBinding)
            {
                this.IsFirstBinding = isFirstBinding;
            }

            public bool IsFirstBinding { get; }

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
