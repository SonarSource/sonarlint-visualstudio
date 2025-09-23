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

using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal sealed class GroupFileViewModel : ViewModelBase, IGroupViewModel
{
    private readonly object @lock = new();

    public GroupFileViewModel(string filePath, ObservableCollection<IIssueViewModel> issues, IThreadHandling threadHandling)
    {
        Title = Path.GetFileName(filePath);
        FilePath = filePath;
        FilteredIssues = issues;
        threadHandling.RunOnUIThread(() => { BindingOperations.EnableCollectionSynchronization(FilteredIssues, @lock); });
    }

    public string Title { get; }
    public string FilePath { get; }
    public ObservableCollection<IIssueViewModel> FilteredIssues { get; }

    public void Dispose() { }
}
