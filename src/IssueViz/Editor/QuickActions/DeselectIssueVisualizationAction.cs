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

using System.Threading;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions
{
    internal class DeselectIssueVisualizationAction : BaseSuggestedAction
    {
        private readonly IAnalysisIssueSelectionService selectionService;

        /// <summary>
        /// <see cref="IssueLocationActionsSource.GetSuggestedActions"/> can return a cached version of the actions, which will show a deselect action even though the issue has already been deselected.
        /// Which is why we need to cache the data of the issue that was selected at the time this action was created.
        /// </summary>
        private readonly string cachedSelectedIssueRuleId;

        public DeselectIssueVisualizationAction(IAnalysisIssueSelectionService selectionService)
        {
            this.selectionService = selectionService;
            cachedSelectedIssueRuleId = selectionService.SelectedIssue.RuleId;
        }

        public override string DisplayText => $"{cachedSelectedIssueRuleId}: hide SonarLint issue visualization";

        public override void Invoke(CancellationToken cancellationToken)
        {
            selectionService.SelectedIssue = null;
        }
    }
}
