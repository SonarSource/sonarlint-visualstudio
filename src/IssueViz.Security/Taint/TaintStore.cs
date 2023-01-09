﻿/*
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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    internal interface ITaintStore : IIssuesStore
    {
        /// <summary>
        /// Removes all existing visualizations and initializes the store to the given collection.
        /// Can be called multiple times.
        /// </summary>
        void Set(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, AnalysisInformation analysisInformation);

        /// <summary>
        /// Returns additional analysis information for the existing visualizations in the store.
        /// </summary>
        AnalysisInformation GetAnalysisInformation();
    }

    [Export(typeof(ITaintStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintStore : ITaintStore
    {
        private IAnalysisIssueVisualization[] taintVulnerabilities = Array.Empty<IAnalysisIssueVisualization>();
        private AnalysisInformation analysisInformation;

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll() => taintVulnerabilities;
        
        public AnalysisInformation GetAnalysisInformation() => analysisInformation;

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public void Set(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, AnalysisInformation analysisInformation)
        {
            if (issueVisualizations == null)
            {
                throw new ArgumentNullException(nameof(issueVisualizations));
            }

            this.analysisInformation = analysisInformation;

            var oldIssues = taintVulnerabilities;
            taintVulnerabilities = issueVisualizations.ToArray();

            NotifyIssuesChanged(oldIssues);
        }

        private void NotifyIssuesChanged(IAnalysisIssueVisualization[] oldIssues)
        {
            var removedIssues = oldIssues.Except(taintVulnerabilities).ToArray();
            var addedIssues = taintVulnerabilities.Except(oldIssues).ToArray();

            if (removedIssues.Any() || addedIssues.Any())
            {
                IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(removedIssues, addedIssues));
            }
        }
    }
}
