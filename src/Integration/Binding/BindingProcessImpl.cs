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
        private readonly ISolutionBindingOperation solutionBindingOperation;
        private readonly IBindingConfigProvider bindingConfigProvider;
        private readonly IExclusionSettingsStorage exclusionSettingsStorage;
        private readonly IEnumerable<Language> languagesToBind;

        public BindingProcessImpl(IHost host,
            BindCommandArgs bindingArgs,
            ISolutionBindingOperation solutionBindingOperation,
            IBindingConfigProvider bindingConfigProvider,
            IExclusionSettingsStorage exclusionSettingsStorage,
            bool isFirstBinding = false)
            : this(host,
                  bindingArgs,
                  solutionBindingOperation,
                  bindingConfigProvider,
                  exclusionSettingsStorage,
                  isFirstBinding,
                  languagesToBind: Language.KnownLanguages)
        { }

        internal /* for testing */ BindingProcessImpl(IHost host,
            BindCommandArgs bindingArgs,
            ISolutionBindingOperation solutionBindingOperation,
            IBindingConfigProvider bindingConfigProvider,
            IExclusionSettingsStorage exclusionSettingsStorage,
            bool isFirstBinding,
            IEnumerable<Language> languagesToBind)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            this.bindingArgs = bindingArgs ?? throw new ArgumentNullException(nameof(bindingArgs));
            this.solutionBindingOperation = solutionBindingOperation ?? throw new ArgumentNullException(nameof(solutionBindingOperation));
            this.bindingConfigProvider = bindingConfigProvider ?? throw new ArgumentNullException(nameof(bindingConfigProvider));
            this.exclusionSettingsStorage = exclusionSettingsStorage ?? throw new ArgumentNullException(nameof(exclusionSettingsStorage));
            this.languagesToBind = languagesToBind ?? throw new ArgumentNullException(nameof(languagesToBind));

            Debug.Assert(bindingArgs.ProjectKey != null);
            Debug.Assert(bindingArgs.ProjectName != null);
            Debug.Assert(bindingArgs.Connection != null);

            this.InternalState = new BindingProcessState(isFirstBinding);
        }

        // This property should not be used by product code outside this class
        internal /* for testing */ BindingProcessState InternalState { get;  }

        #region IBindingTemplate methods

        public async Task<bool> DownloadQualityProfileAsync(IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken)
        {
            var languageList = GetBindingLanguages();

            var languageCount = languageList.Count();
            int currentLanguage = 0;

            foreach (var language in languageList)
            {
                currentLanguage++;

                var progressMessage = string.Format(Strings.DownloadingQualityProfileProgressMessage, language.Name);
                progress?.Report(new FixedStepsProgress(progressMessage, currentLanguage, languageCount));

                var qualityProfileInfo = await TryDownloadQualityProfileAsync(language, cancellationToken);
                
                if (qualityProfileInfo == null)
                {
                    continue; // skip to the next language
                }

                InternalState.QualityProfiles[language] = qualityProfileInfo;

                var bindingConfiguration = QueueWriteBindingInformation();

                // Create the binding configuration for the language
                var bindingConfig = await bindingConfigProvider.GetConfigurationAsync(qualityProfileInfo, language, bindingConfiguration, cancellationToken);
                if (bindingConfig == null)
                {
                    host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                        string.Format(Strings.FailedToCreateBindingConfigForLanguage, language.Name)));
                    return false;
                }

                // TODO - CM: we don't need the dictionary, just the list of configs.
                InternalState.BindingConfigs[language] = bindingConfig;

                host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                    string.Format(Strings.QualityProfileDownloadSuccessfulMessageFormat, qualityProfileInfo.Name, qualityProfileInfo.Key, language.Name)));
            }

            return true;
        }

        /// <summary>
        /// Attempts to fetch the QP for the specified language.
        /// </summary>
        /// <returns>The QP, or null if the language plugin is not available on the server</returns>
        private async Task<SonarQubeQualityProfile> TryDownloadQualityProfileAsync(Language language, CancellationToken cancellationToken)
        {
            // There are valid scenarios in which a language plugin will not be available on the server:
            // 1) the CFamily plugin does not ship in Community edition (nor do any other commerical plugins)
            // 2) a recently added language will not be available in older-but-still-supported SQ versions
            //      e.g. the "secrets" language
            // The unavailability of a language should not prevent binding from succeeding.

            // Note: the historical check that plugins meet a minimum version was removed. 
            // See https://github.com/SonarSource/sonarlint-visualstudio/issues/4272

            var qualityProfileInfo = await WebServiceHelper.SafeServiceCallAsync(() =>
    
            host.SonarQubeService.GetQualityProfileAsync(
                bindingArgs.ProjectKey, bindingArgs.Connection.Organization?.Key, language.ServerLanguage, cancellationToken),
                host.Logger);

            if (qualityProfileInfo == null)
            {
                host.Logger.WriteLine(string.Format(Strings.SubTextPaddingFormat,
                   string.Format(Strings.CannotDownloadQualityProfileForLanguage, language.Name)));
                return null;
            }
            
            return qualityProfileInfo;
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

            return configurationPersister.Persist(bound);
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

        public void PrepareSolutionBinding(CancellationToken cancellationToken)
        {
            this.solutionBindingOperation.Prepare(this.InternalState.BindingConfigs.Values, cancellationToken);
        }

        public bool BindOperationSucceeded => InternalState.BindingOperationSucceeded;

        #endregion

        #region Private methods

        internal /* for testing */ IEnumerable<Language> GetBindingLanguages()
            => languagesToBind;

        #endregion

            #region Workflow state

        internal class BindingProcessState
        {
            public BindingProcessState(bool isFirstBinding)
            {
                this.IsFirstBinding = isFirstBinding;
            }

            public bool IsFirstBinding { get; }

            // TODO - change to simple list of configs
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
