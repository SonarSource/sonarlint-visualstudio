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
using System.IO;
using System.Security;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.WPF;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials;

public class CredentialsViewModel(ConnectionInfo connectionInfo, ISlCoreConnectionAdapter slCoreConnectionAdapter, IProgressReporterViewModel progressReporterViewModel) : ViewModelBase
{
    private SecureString token = new();
    private string selectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;
    private string username;
    private SecureString password = new();

    public ConnectionInfo ConnectionInfo { get; } = connectionInfo;
    public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;
    public ObservableCollection<string> AuthenticationType { get; } = [UiResources.AuthenticationTypeOptionToken, UiResources.AuthenticationTypeOptionCredentials];

    public string SelectedAuthenticationType
    {
        get => selectedAuthenticationType;
        set
        {
            selectedAuthenticationType = value;
            ProgressReporterViewModel.Warning = null;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsTokenAuthentication));
            RaisePropertyChanged(nameof(IsCredentialsAuthentication));
            RaisePropertyChanged(nameof(IsConfirmationEnabled));
            RaisePropertyChanged(nameof(ShouldTokenBeFilled));
            RaisePropertyChanged(nameof(ShouldUsernameBeFilled));
            RaisePropertyChanged(nameof(ShouldPasswordBeFilled));
        }
    }

    public SecureString Token
    {
        get => token;
        set
        {
            token = value;
            RaisePropertyChanged(nameof(IsConfirmationEnabled));
            RaisePropertyChanged(nameof(ShouldTokenBeFilled));
        }
    }

    public string Username
    {
        get => username;
        set
        {
            username = value;
            RaisePropertyChanged(nameof(IsConfirmationEnabled));
            RaisePropertyChanged(nameof(ShouldUsernameBeFilled));
        }
    }

    public SecureString Password
    {
        get => password;
        set
        {
            password = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsConfirmationEnabled));
            RaisePropertyChanged(nameof(ShouldPasswordBeFilled));
        }
    }

    public bool IsTokenAuthentication => SelectedAuthenticationType == UiResources.AuthenticationTypeOptionToken;
    public bool IsCredentialsAuthentication => SelectedAuthenticationType == UiResources.AuthenticationTypeOptionCredentials;
    public bool ShouldTokenBeFilled => IsTokenAuthentication && !IsTokenProvided;
    public bool ShouldUsernameBeFilled => IsCredentialsAuthentication && !IsUsernameProvided;
    public bool ShouldPasswordBeFilled => IsCredentialsAuthentication && !IsPasswordProvided;
    public bool IsConfirmationEnabled => !ProgressReporterViewModel.IsOperationInProgress &&
                                         ( (IsTokenAuthentication && IsTokenProvided) || (IsCredentialsAuthentication && AreCredentialsProvided) );

    private bool IsTokenProvided => IsSecureStringFilled(Token);
    private bool AreCredentialsProvided => IsPasswordProvided && IsUsernameProvided;
    private bool IsUsernameProvided => IsValueFilled(Username);
    private bool IsPasswordProvided => IsSecureStringFilled(Password);
    public string AccountSecurityUrl => ConnectionInfo.ServerType == ConnectionServerType.SonarCloud ? UiResources.SonarCloudAccountSecurityUrl : Path.Combine(ConnectionInfo.Id, UiResources.SonarQubeAccountSecurityUrl);

    private static bool IsValueFilled(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool IsSecureStringFilled(SecureString secureString)
    {
        return !string.IsNullOrWhiteSpace(secureString?.ToUnsecureString());
    }

    internal async Task<bool> ValidateConnectionAsync()
    {
        var validationParams = new TaskToPerformParams<AdapterResponse>(AdapterValidateConnectionAsync, UiResources.ValidatingConnectionProgressText, UiResources.ValidatingConnectionFailedText)
        {
            AfterProgressUpdated = AfterProgressStatusUpdated
        };
        var adapterResponse = await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        return adapterResponse.Success;
    }

    internal async Task<AdapterResponse> AdapterValidateConnectionAsync()
    {
        return await slCoreConnectionAdapter.ValidateConnectionAsync(ConnectionInfo, GetCredentialsModel());
    }

    internal void AfterProgressStatusUpdated()
    {
        RaisePropertyChanged(nameof(IsConfirmationEnabled));
    }
 
    public ICredentialsModel GetCredentialsModel()
    {
       return IsTokenAuthentication ? new TokenCredentialsModel(Token) : new UsernamePasswordModel(Username, Password);
    }
}
