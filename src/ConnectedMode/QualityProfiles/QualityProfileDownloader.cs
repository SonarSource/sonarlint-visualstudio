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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;
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
        private readonly ISolutionBindingOperation solutionBindingOperation;
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
                new SolutionBindingOperation(),
                Language.KnownLanguages)
        { }

        internal /* for testing */ QualityProfileDownloader(
            IBindingConfigProvider bindingConfigProvider,
            IConfigurationPersister configurationPersister,
            IOutOfDateQualityProfileFinder outOfDateQualityProfileFinder,
            ILogger logger,
            ISolutionBindingOperation solutionBindingOperation,
            IEnumerable<Language> languagesToBind)
        {
            this.bindingConfigProvider = bindingConfigProvider;
            this.configurationPersister = configurationPersister;
            this.solutionBindingOperation = solutionBindingOperation;
            this.logger = logger;
            this.languagesToBind = languagesToBind;
            this.outOfDateQualityProfileFinder = outOfDateQualityProfileFinder;
        }

        public async Task<bool> UpdateAsync(BoundSonarQubeProject boundProject, IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken)
        {
            // TODO - CancellableJobRunner
            // TODO - threading
            // TODO - skip downloading up to date QPs

            var isChanged = false;

            EnsureProfilesExistForAllSupportedLanguages(boundProject);

            var outOfDateProfiles = await outOfDateQualityProfileFinder.GetAsync(boundProject, cancellationToken);
            
            var bindingConfigs = new List<IBindingConfig>();

            int currentLanguage = 0;
            var totalLanguages = outOfDateProfiles.Count;

            foreach (var (language, qualityProfileInfo) in outOfDateProfiles)
            {
                currentLanguage++;

                var progressMessage = string.Format(BindingStrings.DownloadingQualityProfileProgressMessage, language.Name);
                progress?.Report(new FixedStepsProgress(progressMessage, currentLanguage, totalLanguages));

                UpdateProfile(boundProject, language, qualityProfileInfo);

                var bindingConfiguration = configurationPersister.Persist(boundProject);

                // Create the binding configuration for the language
                var bindingConfig = await bindingConfigProvider.GetConfigurationAsync(qualityProfileInfo, language, bindingConfiguration, cancellationToken);
                if (bindingConfig == null)
                {
                    // NOTE: this should never happen, binding config should be present for every supported language
                    throw new InvalidOperationException(
                        string.Format(BindingStrings.FailedToCreateBindingConfigForLanguage, language.Name));
                }

                bindingConfigs.Add(bindingConfig);
                isChanged = true;

                logger.WriteLine(string.Format(BindingStrings.SubTextPaddingFormat,
                    string.Format(BindingStrings.QualityProfileDownloadSuccessfulMessageFormat, qualityProfileInfo.Name, qualityProfileInfo.Key, language.Name)));
            }

            if (isChanged)
            {
                solutionBindingOperation.SaveRuleConfiguration(bindingConfigs, cancellationToken);
            }
            else
            {
                logger.WriteLine(string.Format(BindingStrings.SubTextPaddingFormat, BindingStrings.DownloadingQualityProfilesNotNeeded));
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
    }
}
