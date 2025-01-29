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
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

public interface IDiffViewService
{
    bool ShowDiffView(ITextBuffer fileTextBuffer, IReadOnlyList<FixSuggestionChange> changes);
}

[Export(typeof(IDiffViewService))]
[PartCreationPolicy(CreationPolicy.NonShared)]
public class DiffViewService : IDiffViewService
{
    private readonly IDiffViewToolWindowPane diffViewToolWindowPane;
    private readonly ITextViewEditor textViewEditor;

    [ImportingConstructor]
    internal DiffViewService(IToolWindowService toolWindowService, ITextViewEditor textViewEditor)
    {
        this.textViewEditor = textViewEditor;

        diffViewToolWindowPane = toolWindowService.GetToolWindow<DiffViewToolWindowPane, IDiffViewToolWindowPane>();
    }

    public bool ShowDiffView(ITextBuffer fileTextBuffer, IReadOnlyList<FixSuggestionChange> changes) =>
        diffViewToolWindowPane.ShowDiff(new DiffViewViewModel(textViewEditor, fileTextBuffer, changes));
}
