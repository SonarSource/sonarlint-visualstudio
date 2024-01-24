/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    /// <summary>
    /// Callback used by the issue consumer to notify when a snapshot
    /// has changed i.e. the set of issues has changed.
    /// </summary>
    internal delegate void SnapshotChangedHandler(IIssuesSnapshot issuesSnapshot);

    internal interface IIssueConsumerFactory
    {
        /// <summary>
        /// Creates and returns a new issue consumer
        /// </summary>
        /// <remarks>
        /// Instancing: a new issue consumer should be created for each analysis request
        /// i.e. the lifetime of the issue consumer should be tied to that analysis.
        /// </remarks>
        IIssueConsumer Create(ITextDocument textDocument, string projectName, Guid projectGuid, SnapshotChangedHandler onSnapshotChanged);
    }

    [Export(typeof(IIssueConsumerFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal partial class IssueConsumerFactory : IIssueConsumerFactory
    {
        private readonly ISuppressedIssueMatcher suppressedIssueMatcher;
        private readonly IAnalysisIssueVisualizationConverter converter;
        private readonly ILocalHotspotsStoreUpdater localHotspotsStore;

        [ImportingConstructor]
        internal IssueConsumerFactory(ISuppressedIssueMatcher suppressedIssueMatcher, IAnalysisIssueVisualizationConverter converter, ILocalHotspotsStoreUpdater localHotspotsStore)
        {
            this.suppressedIssueMatcher = suppressedIssueMatcher;
            this.converter = converter;
            this.localHotspotsStore = localHotspotsStore;
        }

        public IIssueConsumer Create(ITextDocument textDocument, string projectName, Guid projectGuid, SnapshotChangedHandler onSnapshotChanged)
        {
            var issueHandler = new IssueHandler(textDocument, projectName, projectGuid, suppressedIssueMatcher, onSnapshotChanged, localHotspotsStore);
            var issueConsumer = new AccumulatingIssueConsumer(textDocument.TextBuffer.CurrentSnapshot, textDocument.FilePath, issueHandler.HandleNewIssues, converter);

            return issueConsumer;
        }
    }
}
