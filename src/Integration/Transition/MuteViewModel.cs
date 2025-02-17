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
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Transition;

public class MuteViewModel : ViewModelBase
{
    private readonly IReadOnlyList<StatusViewModel> allStatusViewModels =
    [
        new(SonarQubeIssueTransition.Accept, Strings.MuteWindow_AcceptTitle, Strings.MuteWindow_AcceptContent),
        new(SonarQubeIssueTransition.WontFix, Strings.MuteWindow_WontFixTitle, Strings.MuteWindow_WontFixContent),
        new(SonarQubeIssueTransition.FalsePositive, Strings.MuteWindow_FalsePositiveTitle, Strings.MuteWindow_FalsePositiveContent)
    ];
    private string comment;
    private StatusViewModel selectedStatusViewModel;

    public string Comment
    {
        get => comment;
        set
        {
            comment = value;
            RaisePropertyChanged();
        }
    }

    public StatusViewModel SelectedStatusViewModel
    {
        get => selectedStatusViewModel;
        set
        {
            selectedStatusViewModel = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsSubmitButtonEnabled));
        }
    }

    public bool IsSubmitButtonEnabled => SelectedStatusViewModel != null;

    public ObservableCollection<StatusViewModel> AllowedStatusViewModels { get; set; } = [];

    public void InitializeStatuses(IEnumerable<SonarQubeIssueTransition> transitions)
    {
        allStatusViewModels.ToList().ForEach(vm => vm.IsChecked = false);
        SelectedStatusViewModel = null;

        AllowedStatusViewModels.Clear();
        allStatusViewModels.Where(x => transitions.Contains(x.Transition)).ToList().ForEach(vm => AllowedStatusViewModels.Add(vm));
    }
}
