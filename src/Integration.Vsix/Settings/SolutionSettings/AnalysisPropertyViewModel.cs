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
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

internal class AnalysisPropertyViewModel : ViewModelBase
{
    private string error;
    private string name;
    private string value;

    public string Name
    {
        get => name;
        set
        {
            name = value;
            UpdateValidationError();
            RaisePropertyChanged();
        }
    }

    public string Value
    {
        get => value;
        set
        {
            this.value = value;
            UpdateValidationError();
            RaisePropertyChanged();
        }
    }

    public string Error
    {
        get => error;
        private set
        {
            error = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(Error);

    public AnalysisPropertyViewModel(string name, string value)
    {
        Name = name;
        Value = value;
    }

    private void UpdateValidationError() =>
        Error = CheckIsEmpty(Name) || CheckIsEmpty(Value)
            ? Strings.AddAnalysisPropertyDialog_EmptyErrorMessage
            : null;

    private static bool CheckIsEmpty(string propertyValue) => string.IsNullOrWhiteSpace(propertyValue);
}
