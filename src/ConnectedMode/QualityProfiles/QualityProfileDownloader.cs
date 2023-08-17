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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.QualityProfiles
{
    internal interface IQualityProfileDownloader
    {
        /// <summary>
        /// Ensures that the Quality Profiles for all supported languages are to date
        /// </summary>
        /// <returns>true if there were changes updated, false if everything is up to date</returns>
        /// <exception cref="InvalidOperationException">If binding failed for one of the languages</exception>
        Task<bool> UpdateAsync(BoundSonarQubeProject boundProject, IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken);
    }
    
    [Export(typeof(IQualityProfileDownloader))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class QualityProfileDownloader : IQualityProfileDownloader
    {
        private readonly IBindingConfigProvider bindingConfigProvider;
        private readonly IConfigurationPersister configurationPersister;
        private readonly IOutOfDateQualityProfileFinder outOfDateQualityProfileFinder;

        private readonly ILogger logger;

        private readonly IEnumerable<Language> languagesToBind;

        [ImportingConstructor]
        public QualityProfileDownloader(
            IBindingConfigProvider bindingConfigProvider,
            IConfigurationPersister configurationPersister,
            IOutOfDateQualityProfileFinder outOfDateQualityProfileFinder,
            ILogger logger) :
            this(
                bindingConfigProvider,
                configurationPersister,
                outOfDateQualityProfileFinder, 
                logger,
                Language.KnownLanguages)
        { }

        internal /* for testing */ QualityProfileDownloader(
            IBindingConfigProvider bindingConfigProvider,
            IConfigurationPersister configurationPersister,
            IOutOfDateQualityProfileFinder outOfDateQualityProfileFinder,
            ILogger logger,
            IEnumerable<Language> languagesToBind)
        {
            this.bindingConfigProvider = bindingConfigProvider;
            this.configurationPersister = configurationPersister;
            this.logger = logger;
            this.languagesToBind = languagesToBind;
            this.outOfDateQualityProfileFinder = outOfDateQualityProfileFinder;
        }

        public async Task<bool> UpdateAsync(BoundSonarQubeProject boundProject, IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken)
        {
            var isChanged = false;

            LogWithBindingPrefix(QualityProfilesStrings.UpdatingQualityProfiles);

            EnsureProfilesExistForAllSupportedLanguages(boundProject);

            var outOfDateProfiles = await outOfDateQualityProfileFinder.GetAsync(boundProject, cancellationToken);
            
            int currentLanguage = 0;
            var totalLanguages = outOfDateProfiles.Count;

            LogWithBindingPrefix(string.Format(QualityProfilesStrings.OutOfDateQPsFound, totalLanguages));

            foreach (var (language, qualityProfileInfo) in outOfDateProfiles)
            {
                currentLanguage++;

                var progressMessage = string.Format(QualityProfilesStrings.DownloadingQualityProfileProgressMessage, language.Name);
                progress?.Report(new FixedStepsProgress(progressMessage, currentLanguage, totalLanguages));

                UpdateProfile(boundProject, language, qualityProfileInfo);

                // (1) Save the top-level binding.config file that contains the QP timestamp
                var bindingConfiguration = configurationPersister.Persist(boundProject);

                // Create the binding configuration for the language
                var bindingConfig = await bindingConfigProvider.GetConfigurationAsync(qualityProfileInfo, language, bindingConfiguration, cancellationToken);
                if (bindingConfig == null)
                {
                    // NOTE: this should never happen, binding config should be present for every supported language
                    throw new InvalidOperationException(
                        string.Format(QualityProfilesStrings.FailedToCreateBindingConfigForLanguage, language.Name));
                }

                // (2) Save the language-specific rules config.
                // Note: we've already saved the updated timestamp for the current language in the binding.config file at step (1) above
                // If there is an error between (1) and (2) then the timestamp in the binding.config will show this language
                // is up to date when it is not.
                bindingConfig.Save();
                isChanged = true;

                LogWithBindingPrefix(string.Format(BindingStrings.SubTextPaddingFormat,
                    string.Format(QualityProfilesStrings.QualityProfileDownloadSuccessfulMessageFormat, qualityProfileInfo.Name, qualityProfileInfo.Key, language.Name)));
            }

            if (!isChanged)
            {
                LogWithBindingPrefix(string.Format(QualityProfilesStrings.SubTextPaddingFormat, QualityProfilesStrings.DownloadingQualityProfilesNotNeeded));
            }
            
            return isChanged;
        }

        /// <summary>
        /// Ensures that the bound project has a profile entry for every supported language
        /// </summary>
        /// <remarks>If we add support for new language in the future, this method will make sure it's
        /// Quality Profile is fetched next time an update is triggered</remarks>
        private void EnsureProfilesExistForAllSupportedLanguages(BoundSonarQubeProject boundProject)
        {
            if (boundProject.Profiles == null)
            {
                boundProject.Profiles = new Dictionary<Language, ApplicableQualityProfile>();
            }

            foreach (var language in languagesToBind)
            {
                if (!boundProject.Profiles.ContainsKey(language))
                {
                    boundProject.Profiles[language] = new ApplicableQualityProfile
                    {
                        ProfileKey = null,
                        ProfileTimestamp = DateTime.MinValue,
                    };
                }
            }
        }

        private static void UpdateProfile(BoundSonarQubeProject boundSonarQubeProject, Language language, SonarQubeQualityProfile serverProfile)
        {
            boundSonarQubeProject.Profiles[language] = new ApplicableQualityProfile
            {
                ProfileKey = serverProfile.Key, ProfileTimestamp = serverProfile.TimeStamp
            };
        }

        private void LogWithBindingPrefix(string text)
            => logger.WriteLine(QualityProfilesStrings.QPMessagePrefix + text);
    }
}
