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

using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

public class ChangeViewModel : ViewModelBase
{
    public FixSuggestionChange Change { get; }
    public string AfterPreview { get; }
    public string BeforePreview { get; }

    public bool IsSelected
    {
        get => Change.IsAccepted;
        set
        {
            Change.IsAccepted = value;
            RaisePropertyChanged();
        }
    }

    public ChangeViewModel(FixSuggestionChange change)
    {
        Change = change;
        IsSelected = true;
        BeforePreview = ToSingleLine(change.BeforeText);
        AfterPreview = ToSingleLine(change.AfterText);
    }

    private static string ToSingleLine(string text) => text.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty);
}
