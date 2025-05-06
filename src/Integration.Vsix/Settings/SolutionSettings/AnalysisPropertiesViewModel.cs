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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

internal class AnalysisPropertiesViewModel : ViewModelBase
{
    private AnalysisPropertyViewModel selectedProperty;

    public bool IsAnyPropertySelected => SelectedProperty != null;
    public ObservableCollection<AnalysisPropertyViewModel> AnalysisProperties { get; } = [];

    public AnalysisPropertyViewModel SelectedProperty
    {
        get => selectedProperty;
        set
        {
            selectedProperty = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsAnyPropertySelected));
        }
    }

    internal void AddProperty(string property, string value)
    {
        var newSetting = new AnalysisPropertyViewModel(property, value);
        AnalysisProperties.Add(newSetting);
        SelectedProperty = newSetting;
    }

    internal void RemoveSelectedProperty()
    {
        if (SelectedProperty == null)
        {
            return;
        }
        AnalysisProperties.Remove(SelectedProperty);
        SelectedProperty = null;
    }
}
