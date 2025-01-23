/*
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

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

public interface IDiffViewService
{
    bool ShowDiffView(FixSuggestionDetails fixSuggestionDetails, ChangeModel before, ChangeModel after);
}

[Export(typeof(IDiffViewService))]
[PartCreationPolicy(CreationPolicy.NonShared)]
public class DiffViewService : IDiffViewService
{
    private readonly ITextBufferFactoryService textBufferFactoryService;
    private readonly IDiffViewToolWindowPane diffViewToolWindowPane;

    [ImportingConstructor]
    internal DiffViewService(
        IToolWindowService toolWindowService,
        ITextBufferFactoryService textBufferFactoryService)
    {
        this.textBufferFactoryService = textBufferFactoryService;

        diffViewToolWindowPane = toolWindowService.GetToolWindow<DiffViewToolWindowPane, IDiffViewToolWindowPane>();
    }

    public bool ShowDiffView(FixSuggestionDetails fixSuggestionDetails, ChangeModel before, ChangeModel after) =>
        diffViewToolWindowPane.ShowDiff(fixSuggestionDetails, CreateTextBuffer(before), CreateTextBuffer(after));

    private ITextBuffer CreateTextBuffer(ChangeModel change) => textBufferFactoryService.CreateTextBuffer(change.Text, change.ContentType);
}
