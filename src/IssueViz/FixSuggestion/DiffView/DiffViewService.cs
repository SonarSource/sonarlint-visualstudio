﻿/*
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
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

internal interface IDiffViewService
{
    FinalizedFixSuggestionChange[] ShowDiffView(ITextBuffer fileTextBuffer, IReadOnlyList<FixSuggestionChange> changes);
}

[Export(typeof(IDiffViewService))]
[PartCreationPolicy(CreationPolicy.NonShared)]
internal class DiffViewService : IDiffViewService
{
    private readonly Lazy<IDiffViewToolWindowPane> diffViewToolWindowPane;
    private readonly ITextViewEditor textViewEditor;

    [ImportingConstructor]
    internal DiffViewService(IToolWindowService toolWindowService, ITextViewEditor textViewEditor)
    {
        this.textViewEditor = textViewEditor;

        diffViewToolWindowPane = new (() => toolWindowService.GetToolWindow<DiffViewToolWindowPane, IDiffViewToolWindowPane>());
    }

    public FinalizedFixSuggestionChange[] ShowDiffView(ITextBuffer fileTextBuffer, IReadOnlyList<FixSuggestionChange> changes) =>
        diffViewToolWindowPane.Value.ShowDiff(new DiffViewViewModel(textViewEditor, fileTextBuffer, changes));
}
