/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;

[Export(typeof(IQuickFixService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class QuickFixService : IQuickFixService
{
    private readonly IDocumentTrackerEx documentTracker;
    private readonly IQuickFixApplicationLogic quickFixApplicationLogic;

    [ImportingConstructor]
    public QuickFixService(
        IDocumentTrackerEx documentTracker,
        IQuickFixApplicationLogic quickFixApplicationLogic)
    {
        this.documentTracker = documentTracker;
        this.quickFixApplicationLogic = quickFixApplicationLogic;
    }

    public bool CanBeApplied(IQuickFixApplication quickFix, string filePath)
    {
        if (!documentTracker.TryGetCurrentSnapshot(filePath, out var snapshot))
        {
            return false;
        }
        return quickFixApplicationLogic.CanBeApplied(quickFix, snapshot);
    }

    public async Task<bool> ApplyAsync(IQuickFixApplication quickFix, string filePath,
        IAnalysisIssueVisualization issueViz, CancellationToken cancellationToken = default)
    {
        if (!documentTracker.TryGetCurrentSnapshot(filePath, out var snapshot))
        {
            return false;
        }
        return await quickFixApplicationLogic.ApplyAsync(quickFix, snapshot, issueViz, cancellationToken);
    }
}
