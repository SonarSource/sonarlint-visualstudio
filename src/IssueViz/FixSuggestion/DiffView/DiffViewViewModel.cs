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

using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

public class DiffViewViewModel : ViewModelBase
{
    private readonly ITextViewEditor textViewEditor;
    private bool allChangesSelected;

    public ITextBuffer TextBuffer { get; }
    public List<ChangeViewModel> ChangeViewModels { get; }
    public string FilePath { get; }
    public string FileName { get; }
    public ITextBuffer Before { get; private set; }
    public ITextBuffer After { get; private set; }

    public bool AllChangesSelected
    {
        get => allChangesSelected;
        set
        {
            if (allChangesSelected == value)
            {
                return;
            }
            allChangesSelected = value;
            ChangeViewModels.ForEach(vm => vm.IsSelected = value);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsApplyEnabled));
        }
    }

    public bool IsApplyEnabled => ChangeViewModels.Any(vm => vm.IsSelected);

    public DiffViewViewModel(
        ITextViewEditor textViewEditor,
        ITextBuffer textBuffer,
        IReadOnlyList<FixSuggestionChange> changes)
    {
        this.textViewEditor = textViewEditor;
        TextBuffer = textBuffer;
        ChangeViewModels = changes.Select(change => new ChangeViewModel(change)).ToList();
        CalculateAllChangesSelected();
        FilePath = textBuffer.GetFilePath();
        FileName = Path.GetFileName(FilePath);
    }

    public void DeclineAllChanges() => AllChangesSelected = false;

    public void InitializeBeforeAndAfter()
    {
        Before = textViewEditor.CreateTextBuffer(TextBuffer.CurrentSnapshot.GetText(), TextBuffer.ContentType);
        CalculateAfter();
    }

    public void CalculateAfter()
    {
        After = textViewEditor.CreateTextBuffer(TextBuffer.CurrentSnapshot.GetText(), TextBuffer.ContentType);

        var selectedChanges = ChangeViewModels.Where(vm => vm.IsSelected).Select(vm => vm.Change).ToList();
        if (selectedChanges.Any())
        {
            textViewEditor.ApplyChanges(After, selectedChanges, abortOnOriginalTextChanged: false);
        }
    }

    public void GoToChangeLocation(ITextView textView, ChangeViewModel changeViewModel) => textViewEditor.FocusLine(textView, changeViewModel.Change.BeforeStartLine);

    /// <summary>
    /// Calculating <see cref="AllChangesSelected"/> is needed to prevent subscribing to PropertyChanged events for each <see cref="ChangeViewModel"/> and then to take care to unsubscribe and avoid memory leaks
    /// It also can't be a computed property, because it is bound to the checkbox in the view
    /// </summary>
    public void CalculateAllChangesSelected()
    {
        allChangesSelected = ChangeViewModels.All(vm => vm.IsSelected);
        RaisePropertyChanged(nameof(AllChangesSelected));
        RaisePropertyChanged(nameof(IsApplyEnabled));
    }
}
