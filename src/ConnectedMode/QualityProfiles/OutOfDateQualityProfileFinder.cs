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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        Task<IEnumerable<(Language language, SonarQubeQualityProfile qualityProfile)>> GetAsync(
            BoundSonarQubeProject sonarQubeProject,
            CancellationToken cancellationToken);
    }

    [Export(typeof(IOutOfDateQualityProfileFinder))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class OutOfDateQualityProfileFinder : IOutOfDateQualityProfileFinder
    {
        private readonly ISonarQubeService sonarQubeService;

        [ImportingConstructor]
        public OutOfDateQualityProfileFinder(ISonarQubeService sonarQubeService)
        {
            this.sonarQubeService = sonarQubeService;
        }

        public async Task<IEnumerable<(Language language, SonarQubeQualityProfile qualityProfile)>> GetAsync(
            BoundSonarQubeProject sonarQubeProject,
            CancellationToken cancellationToken)
        {
            var sonarQubeQualityProfiles =
                await sonarQubeService.GetAllQualityProfilesAsync(sonarQubeProject.ProjectKey,
                    sonarQubeProject.Organization.Key,
                    cancellationToken);

            return sonarQubeQualityProfiles
                .Select(serverQualityProfile => 
                    (language: Language.GetLanguageFromLanguageKey(serverQualityProfile.Language),
                    qualityProfile: serverQualityProfile))
                .Where(languageAndQp =>
                    IsLocalQPOutOfDate(sonarQubeProject, languageAndQp.language, languageAndQp.qualityProfile));
        }

        private static bool IsLocalQPOutOfDate(BoundSonarQubeProject sonarQubeProject, Language language,
            SonarQubeQualityProfile serverQualityProfile)
        {
            if (language == default)
            {
                return false;
            }

            // if we know the language, it's in the dictionary
            Debug.Assert(sonarQubeProject.Profiles.ContainsKey(language));

            var localQualityProfile = sonarQubeProject.Profiles[language];

            return !serverQualityProfile.Key.Equals(localQualityProfile.ProfileKey)
                   || serverQualityProfile.TimeStamp > localQualityProfile.ProfileTimestamp;
        }
    }
}
