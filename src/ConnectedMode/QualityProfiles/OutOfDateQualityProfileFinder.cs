/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.QualityProfiles
{
    internal interface IOutOfDateQualityProfileFinder
    {
        /// <summary>
        /// Gives the list of outdated quality profiles based on the existing ones from <see cref="BoundSonarQubeProject.Profiles"/>
        /// </summary>
        Task<IReadOnlyCollection<(Language language, SonarQubeQualityProfile qualityProfile)>> GetAsync(
            BoundServerProject sonarQubeProject,
            CancellationToken cancellationToken);
    }

    [Export(typeof(IOutOfDateQualityProfileFinder))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class OutOfDateQualityProfileFinder : IOutOfDateQualityProfileFinder
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILanguageProvider languageProvider;

        [ImportingConstructor]
        public OutOfDateQualityProfileFinder(ISonarQubeService sonarQubeService, ILanguageProvider languageProvider)
        {
            this.sonarQubeService = sonarQubeService;
            this.languageProvider = languageProvider;
        }

        public async Task<IReadOnlyCollection<(Language language, SonarQubeQualityProfile qualityProfile)>> GetAsync(
            BoundServerProject sonarQubeProject,
            CancellationToken cancellationToken)
        {
            var sonarQubeQualityProfiles =
                await sonarQubeService.GetAllQualityProfilesAsync(sonarQubeProject.ServerProjectKey,
                    (sonarQubeProject.ServerConnection as ServerConnection.SonarCloud)?.OrganizationKey,
                    cancellationToken);

            return sonarQubeQualityProfiles
                .Select(serverQualityProfile =>
                    (language: languageProvider.GetLanguageFromLanguageKey(serverQualityProfile.Language),
                        qualityProfile: serverQualityProfile))
                .Where(languageAndQp =>
                    IsLocalQPOutOfDate(sonarQubeProject, languageAndQp.language, languageAndQp.qualityProfile))
                .ToArray();
        }

        private static bool IsLocalQPOutOfDate(
            BoundServerProject sonarQubeProject,
            Language language,
            SonarQubeQualityProfile serverQualityProfile)
        {
            if (language == null || !sonarQubeProject.Profiles.TryGetValue(language, out var localQualityProfile))
            {
                return false;
            }

            return !serverQualityProfile.Key.Equals(localQualityProfile.ProfileKey)
                   || serverQualityProfile.TimeStamp > localQualityProfile.ProfileTimestamp;
        }
    }
}
