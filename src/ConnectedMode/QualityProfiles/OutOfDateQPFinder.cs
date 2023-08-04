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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.QualityProfiles
{
    internal interface IOutOfDateQPFinder
    {
        /// <summary>
        /// When in Connected Mode, returns the languages for which the Quality Profile
        /// needs to be updated
        /// </summary>
        Task<IEnumerable<Language>> GetAsync(CancellationToken cancellationToken);
    }

    [Export(typeof(IOutOfDateQPFinder))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class OutOfDateQPFinder : IOutOfDateQPFinder
    {
        private readonly IConfigurationProvider configProvider;
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;

        [ImportingConstructor]
        public OutOfDateQPFinder(IConfigurationProvider configProvider,
            ISonarQubeService sonarQubeService,
            ILogger logger)
        {
            this.configProvider = configProvider;
            this.sonarQubeService = sonarQubeService;
            this.logger = logger;
        }

        public async Task<IEnumerable<Language>> GetAsync(CancellationToken cancellationToken)
        {
            var languagesToUpdate = new List<Language>();

            var config = configProvider.GetConfiguration();
            foreach (var profile in config.Project.Profiles)
            {
                logger.LogQPVerbose(profile.Key, "Checking if Quality Profile has changed");
                if (await IsUpdateRequiredAsync(config, profile, cancellationToken))
                {
                    languagesToUpdate.Add(profile.Key);
                    logger.LogQPVerbose(profile.Key, "Quality profile update is required");
                }
                else
                {
                    logger.LogQPVerbose(profile.Key, "Quality profile is up-to-date");
                }
            }

            return languagesToUpdate;
        }

        private async Task<bool> IsUpdateRequiredAsync(BindingConfiguration config, KeyValuePair<Language, ApplicableQualityProfile> profile, CancellationToken cancellationToken)
        {
            Debug.Assert(profile.Value != null, "Not expecting profile to be null. Language: " + profile.Key);

            var serverProfileInfo = await sonarQubeService.GetQualityProfileAsync(config.Project.ProjectKey,
                config.Project.Organization?.Key,
                profile.Key.ServerLanguage, CancellationToken.None);

            return HasProfileChanged(serverProfileInfo, profile.Value);
        }

        private bool HasProfileChanged(SonarQubeQualityProfile newProfileInfo, ApplicableQualityProfile oldProfileInfo)
        {
            if (!SonarQubeQualityProfile.KeyComparer.Equals(oldProfileInfo.ProfileKey, newProfileInfo.Key))
            {
                logger.LogQPVerbose($"A different Quality Profile is being used: old: {oldProfileInfo.ProfileKey}, new: {oldProfileInfo.ProfileKey}");
                return true;
            }

            if (oldProfileInfo.ProfileTimestamp != newProfileInfo.TimeStamp)
            {
                logger.LogQPVerbose($"Quality Profile has been updated: old: {oldProfileInfo.ProfileKey}, new: {oldProfileInfo.ProfileKey}");
                return true;
            }

            return false;
        }
    }
}
