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
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.RuleSets
{
    internal sealed class QualityProfileManager : IDisposable
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IQualityProfileProvider rulesetProvider;
        private readonly Dictionary<Language, QualityProfile> cachedQualityProfiles = new Dictionary<Language, QualityProfile>();

        private bool isDisposed;

        public QualityProfileManager(IActiveSolutionBoundTracker activeSolutionBoundTracker, IQualityProfileProvider rulesetProvider)
        {
            if (activeSolutionBoundTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionBoundTracker));
            }

            if (rulesetProvider == null)
            {
                throw new ArgumentNullException(nameof(rulesetProvider));
            }

            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.rulesetProvider = rulesetProvider;

            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

            if (activeSolutionBoundTracker.IsActiveSolutionBound)
            {
                FetchRuleSetsForCurrentlyBoundSonarQubeProject(null);
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            this.activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
            this.cachedQualityProfiles.Clear();

            isDisposed = true;
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            if (!e.IsBound)
            {
                cachedQualityProfiles.Clear();
            }
            else
            {
                FetchRuleSetsForCurrentlyBoundSonarQubeProject(null);
            }
        }

        private void FetchRuleSetsForCurrentlyBoundSonarQubeProject(BoundSonarQubeProject boundProject)
        {
            foreach (var language in Language.SupportedLanguages)
            {
                var qualityProfile = rulesetProvider.GetQualityProfile(boundProject, language);
                // TODO: What to do if qualityProfile is null?
                cachedQualityProfiles.Add(qualityProfile.Language, qualityProfile);
            }
        }
    }
}
