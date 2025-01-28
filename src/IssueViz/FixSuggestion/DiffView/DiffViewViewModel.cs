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

using System.IO;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

public class DiffViewViewModel : ViewModelBase
{
    private readonly ITextViewEditor textViewEditor;
    public ITextBuffer TextBuffer { get; }
    public List<ChangeViewModel> ChangeViewModels { get; }
    public string FilePath { get; }
    public string FileName { get; }
    public ITextBuffer Before { get; private set; }
    public ITextBuffer After { get; private set; }

    public DiffViewViewModel(
        ITextViewEditor textViewEditor,
        ITextBuffer textBuffer,
        List<ChangesDto> changesDtos)
    {
        this.textViewEditor = textViewEditor;
        TextBuffer = textBuffer;
        ChangeViewModels = changesDtos.Select(dto => new ChangeViewModel(dto, true)).ToList();
        FilePath = textBuffer.GetFilePath();
        FileName = Path.GetFileName(FilePath);
    }

    public void InitializeBeforeAndAfter()
    {
        Before = textViewEditor.CreateTextBuffer(TextBuffer.CurrentSnapshot.GetText(), TextBuffer.ContentType);
        CalculateAfter();
    }

    public void CalculateAfter()
    {
        After = textViewEditor.CreateTextBuffer(TextBuffer.CurrentSnapshot.GetText(), TextBuffer.ContentType);

        var selectedChangesDtos = ChangeViewModels.Where(vm => vm.IsSelected).Select(vm => vm.ChangeDto).ToList();
        if (selectedChangesDtos.Any())
        {
            textViewEditor.ApplyChanges(After, selectedChangesDtos, abortOnOriginalTextChanged: false);
        }
    }
}
