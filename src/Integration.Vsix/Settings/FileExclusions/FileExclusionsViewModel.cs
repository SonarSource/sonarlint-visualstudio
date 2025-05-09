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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.FileExclusions;

internal class FileExclusionsViewModel(
    IBrowserService browserService,
    IUserSettingsProvider userSettingsProvider,
    IGlobalUserSettingsUpdater globalUserSettingsUpdater,
    ISolutionUserSettingsUpdater solutionUserSettingsUpdater,
    FileExclusionScope scope = FileExclusionScope.Global)
    : ViewModelBase
{
    private ExclusionViewModel selectedExclusion;

    public bool IsAnyExclusionSelected => SelectedExclusion != null;
    public ObservableCollection<ExclusionViewModel> Exclusions { get; } = [];

    public ExclusionViewModel SelectedExclusion
    {
        get => selectedExclusion;
        set
        {
            selectedExclusion = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsAnyExclusionSelected));
        }
    }

    public void InitializeExclusions()
    {
        Exclusions.Clear();

        var exclusionViewModels = scope == FileExclusionScope.Global
            ? userSettingsProvider.UserSettings.AnalysisSettings.GlobalFileExclusions.Select(ex => new ExclusionViewModel(ex))
            : userSettingsProvider.UserSettings.AnalysisSettings.SolutionFileExclusions.Select(ex => new ExclusionViewModel(ex));
        exclusionViewModels.ToList().ForEach(vm => Exclusions.Add(vm));

        SelectedExclusion = Exclusions.FirstOrDefault();
    }

    public void SaveExclusions()
    {
        var exclusionsToSave = Exclusions.Where(vm => vm.Error == null).Select(vm => vm.Pattern);

        if (scope == FileExclusionScope.Global)
        {
            globalUserSettingsUpdater.UpdateGlobalFileExclusions(exclusionsToSave);
        }
        else
        {
            solutionUserSettingsUpdater.UpdateSolutionFileExclusions(exclusionsToSave);
        }
    }

    internal void ViewInBrowser(string uri) => browserService.Navigate(uri);

    internal void AddExclusion(string pattern)
    {
        var newExclusion = new ExclusionViewModel(pattern);
        Exclusions.Add(newExclusion);
        SelectedExclusion = newExclusion;
    }

    internal void RemoveExclusion()
    {
        if (SelectedExclusion == null)
        {
            return;
        }
        Exclusions.Remove(SelectedExclusion);
        SelectedExclusion = null;
    }
}

public enum FileExclusionScope
{
    Global,
    Solution
}
