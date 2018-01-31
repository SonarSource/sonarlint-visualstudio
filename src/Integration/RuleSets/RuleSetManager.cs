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
using System.Collections.Concurrent;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.RuleSets
{
    internal sealed class RuleSetManager : IDisposable
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IProjectSystemHelper projectSystemHelper;
        private readonly IRuleSetProvider rulesetProvider;

        internal /* for testing purposes */ readonly ConcurrentDictionary<Language, SonarRuleSet> cachedRuleSets =
            new ConcurrentDictionary<Language, SonarRuleSet>();

        public RuleSetManager(IActiveSolutionBoundTracker activeSolutionBoundTracker, IProjectSystemHelper projectSystemHelper,
            IRuleSetProvider rulesetProvider)
        {
            if (activeSolutionBoundTracker == null)
            {
                throw new ArgumentNullException(nameof(activeSolutionBoundTracker));
            }

            if (projectSystemHelper == null)
            {
                throw new ArgumentNullException(nameof(projectSystemHelper));
            }

            if (rulesetProvider == null)
            {
                throw new ArgumentNullException(nameof(rulesetProvider));
            }

            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.projectSystemHelper = projectSystemHelper;
            this.rulesetProvider = rulesetProvider;

            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

            if (activeSolutionBoundTracker.IsActiveSolutionBound)
            {
                FetchRuleSetForCurrentProjects();
            }

            // TODO: Find a way to listen to projects added to the solution (in case it is of a language we haven't yet fetched)
        }

        public void Dispose()
        {
            this.activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
            cachedRuleSets.Clear();
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            if (!e.IsBound)
            {
                cachedRuleSets.Clear();
            }
            else
            {
                FetchRuleSetForCurrentProjects();
            }
        }

        private void FetchRuleSetForCurrentProjects()
        {
            this.projectSystemHelper.GetFilteredSolutionProjects()
                .Select(p => new { Project = p, Language = Language.ForProject(p) })
                .Where(tuple => tuple.Language != Language.Unknown)
                .GroupBy(tuple => tuple.Language)
                .Select(tuple => tuple.First())
                .Select(tuple => rulesetProvider.GetRuleSet(null, tuple.Language)) // TODO: Provide the BoundSonarQubeProject
                .ToList()
                .ForEach(ruleset => cachedRuleSets.AddOrUpdate(ruleset.Language, ruleset, (l, r) => r));
        }
    }
}
