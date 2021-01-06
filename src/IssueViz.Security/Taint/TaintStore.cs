/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    internal interface ITaintStore : IDisposable
    {
        ReadOnlyObservableCollection<IAnalysisIssueVisualization> GetAll();

        /// <summary>
        /// Removes all existing visualizations and initializes the store to the given collection.
        /// Can be called multiple times.
        /// </summary>
        void Set(IEnumerable<IAnalysisIssueVisualization> issueVisualizations);
    }

    [Export(typeof(ITaintStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintStore : ITaintStore
    {
        private readonly ObservableCollection<IAnalysisIssueVisualization> taintVulnerabilities;
        private readonly ReadOnlyObservableCollection<IAnalysisIssueVisualization> readOnlyWrapper;
        private readonly IDisposable unregisterCallback;

        [ImportingConstructor]
        public TaintStore(IIssueStoreObserver issueStoreObserver)
        {
            taintVulnerabilities = new ObservableCollection<IAnalysisIssueVisualization>();
            readOnlyWrapper = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(taintVulnerabilities);
            unregisterCallback = issueStoreObserver.Register(readOnlyWrapper);
        }

        public ReadOnlyObservableCollection<IAnalysisIssueVisualization> GetAll() => readOnlyWrapper;

        public void Set(IEnumerable<IAnalysisIssueVisualization> issueVisualizations)
        {
            if (issueVisualizations == null)
            {
                throw new ArgumentNullException(nameof(issueVisualizations));
            }

            taintVulnerabilities.RemoveAll();

            foreach (var issueViz in issueVisualizations)
            {
                taintVulnerabilities.Add(issueViz);
            }
        }

        public void Dispose()
        {
            unregisterCallback.Dispose();
        }
    }
}
