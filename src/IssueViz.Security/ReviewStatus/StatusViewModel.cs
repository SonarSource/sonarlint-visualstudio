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

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

public class StatusViewModel<T>(
    T status,
    string title,
    string description,
    bool isCommentRequired) : ViewModelBase, IStatusViewModel where T : struct, Enum
{
    private bool isChecked;

    public T Status { get; } = status;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public bool IsCommentRequired { get; } = isCommentRequired;

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            isChecked = value;
            RaisePropertyChanged();
        }
    }

    public P GetCurrentStatus<P>() where P : struct, Enum
    {
        if (typeof(P) != typeof(T))
        {
            throw new InvalidOperationException($"Cannot get status of type {typeof(P)} from {nameof(IStatusViewModel)} of type {typeof(T)}.");
        }

        return (P)(object)Status;
    }
}
