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
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;

public class OrganizationSelectionViewModel(ICredentialsModel credentialsModel, ISlCoreConnectionAdapter connectionAdapter, IProgressReporterViewModel progressReporterViewModel) : ViewModelBase
{
    /// <summary>
    /// The <see cref="ConnectionInfo"/> that is used to connect to the server, whose <see cref="ConnectionInfo.Id"/> can be different from the <see cref="SelectedOrganization"/>
    /// due to the fact that an organization key can also be entered manually rather than selected form the list of <see cref="Organizations"/>.
    /// </summary>
    public ConnectionInfo ConnectionInfo { get; private set; } = new(null, ConnectionServerType.SonarCloud);
    public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;

    public OrganizationDisplay SelectedOrganization
    {
        get => selectedOrganization;
        set
        {
            selectedOrganization = value;
            UpdateConnectionInfo(selectedOrganization?.Key);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsValidSelectedOrganization));
        }
    }

    public bool IsValidSelectedOrganization => SelectedOrganization is { Key: var key } && !string.IsNullOrWhiteSpace(key);
    public ObservableCollection<OrganizationDisplay> Organizations { get; } = [];
    public bool NoOrganizationExists => Organizations.Count == 0;

    private OrganizationDisplay selectedOrganization;

    public void AddOrganization(OrganizationDisplay organization)
    {
        Organizations.Add(organization);
        RaisePropertyChanged(nameof(NoOrganizationExists));
    }

    public async Task LoadOrganizationsAsync()
    {
        var organizationLoadingParams = new TaskToPerformParams<AdapterResponseWithData<List<OrganizationDisplay>>>(
            AdapterLoadOrganizationsAsync,
            UiResources.LoadingOrganizationsProgressText, 
            UiResources.LoadingOrganizationsFailedText)
        {
            AfterSuccess = UpdateOrganizations
        };
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(organizationLoadingParams);
    }

    internal async Task<AdapterResponseWithData<List<OrganizationDisplay>>> AdapterLoadOrganizationsAsync()
    {
       return await connectionAdapter.GetOrganizationsAsync(credentialsModel);
    }

    internal void UpdateOrganizations(AdapterResponseWithData<List<OrganizationDisplay>> responseWithData)
    {
        Organizations.Clear();
        responseWithData.ResponseData.ForEach(AddOrganization);
        RaisePropertyChanged(nameof(NoOrganizationExists));
    }

    internal async Task<bool> ValidateConnectionForOrganizationAsync(string organizationKey, string warningText)
    {
        var connectionInfoToValidate = ConnectionInfo with { Id = organizationKey };
        var validationParams = new TaskToPerformParams<AdapterResponse>(
            async () => await connectionAdapter.ValidateConnectionAsync(connectionInfoToValidate, credentialsModel),
            UiResources.ValidatingConnectionProgressText, 
            warningText);
        var adapterResponse = await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        return adapterResponse.Success;
    }

    public void UpdateConnectionInfo(string organizationKey)
    {
        ConnectionInfo = ConnectionInfo with { Id = organizationKey };
    }
}
