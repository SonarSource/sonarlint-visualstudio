/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

namespace SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;

public record OrganizationDisplay(string Key, string Name);

public class OrganizationSelectionViewModel : ViewModelBase
{
    private OrganizationDisplay selectedOrganizationKey;
    private bool isValidOrganizationKey;

    public OrganizationDisplay SelectedOrganizationKey
    {
        get => selectedOrganizationKey;
        set
        {
            selectedOrganizationKey = value;
            IsValidOrganizationKey = value is { Key: not null };
            RaisePropertyChanged();
        }
    }

    public bool IsValidOrganizationKey
    {
        get => isValidOrganizationKey;
        set
        {
            isValidOrganizationKey = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<OrganizationDisplay> Organizations { get; }

    public OrganizationSelectionViewModel(List<(string organizationKey, string organizationName)> organizations)
    {
        Organizations = new ObservableCollection<OrganizationDisplay>(organizations.Select(x =>  new OrganizationDisplay(x.organizationKey, x.organizationName)));
        // RaisePropertyChanged(nameof(Organizations));
    }
}
