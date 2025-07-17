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
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

public class ChangeStatusViewModel<T> : ViewModelBase, IChangeStatusViewModel where T : struct, Enum
{
    private readonly IEnumerable<T> statusesWithMandatoryComment;
    private readonly IReadOnlyList<StatusViewModel<T>> allStatusViewModels;
    private IStatusViewModel selectedStatusViewModel;
    private string comment;
    private string validationError;

    public ChangeStatusViewModel(
        T currentStatus,
        IEnumerable<T> allowedStatuses,
        IEnumerable<T> statusesWithMandatoryComment,
        IReadOnlyList<StatusViewModel<T>> allStatusViewModels,
        bool showComment)
    {
        this.statusesWithMandatoryComment = statusesWithMandatoryComment;
        this.allStatusViewModels = allStatusViewModels;
        InitializeStatuses(allowedStatuses);
        InitializeCurrentStatus(currentStatus);
        ShowComment = showComment;
    }

    public IStatusViewModel SelectedStatusViewModel
    {
        get => selectedStatusViewModel;
        set
        {
            selectedStatusViewModel = value;
            RaisePropertyChanged();
            // order matters here, we want to validate the comment before checking if the submit button is enabled
            RaisePropertyChanged(nameof(Comment));
            RaisePropertyChanged(nameof(IsSubmitButtonEnabled));
        }
    }

    public string Comment
    {
        get => comment;
        set
        {
            comment = value;
            RaisePropertyChanged();
        }
    }

    public string this[string columnName]
    {
        get
        {
            validationError = null;
            if (columnName == nameof(Comment) && string.IsNullOrEmpty(Comment) && IsCommentRequired())
            {
                validationError = Resources.CommentRequiredErrorMessage;
            }
            return validationError;
        }
    }

    public string Error => validationError;
    public bool ShowComment { get; }
    public bool IsSubmitButtonEnabled => SelectedStatusViewModel != null && this[nameof(Comment)] is null;
    public ObservableCollection<IStatusViewModel> AllowedStatusViewModels { get; set; } = [];

    private void InitializeStatuses(IEnumerable<T> allowedStatuses)
    {
        AllowedStatusViewModels.Clear();
        allStatusViewModels.ToList().ForEach(vm => vm.IsChecked = false);
        allStatusViewModels.Where(x => allowedStatuses.Contains(x.Status)).ToList().ForEach(vm => AllowedStatusViewModels.Add(vm));
    }

    private void InitializeCurrentStatus(T currentStatus)
    {
        SelectedStatusViewModel = AllowedStatusViewModels.FirstOrDefault(x => Equals(x.GetCurrentStatus<T>(), currentStatus));
        if (SelectedStatusViewModel == null)
        {
            return;
        }
        SelectedStatusViewModel.IsChecked = true;
    }

    private bool IsCommentRequired() => statusesWithMandatoryComment.Any(x => SelectedStatusViewModel.HasStatus(x));
}
