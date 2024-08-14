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
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.WPF;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials;

public class CredentialsViewModel(Connection connection) : ViewModelBase
{
    private string token;
    private string selectedAuthenticationType = UiResources.AuthenticationTypeOptionToken;
    private string username;
    private string password;

    public Connection Connection { get; } = connection;
    public ObservableCollection<string> AuthenticationType { get; } = [UiResources.AuthenticationTypeOptionToken, UiResources.AuthenticationTypeOptionCredentials];

    public string SelectedAuthenticationType
    {
        get => selectedAuthenticationType;
        set
        {
            selectedAuthenticationType = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsTokenAuthentication));
            RaisePropertyChanged(nameof(IsCredentialsAuthentication));
            RaisePropertyChanged(nameof(IsConfirmationEnabled));
            RaisePropertyChanged(nameof(ShouldTokenBeFilled));
            RaisePropertyChanged(nameof(ShouldUsernameBeFilled));
            RaisePropertyChanged(nameof(ShouldPasswordBeFilled));
        }
    }

    public string Token
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

    public string Password
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
    public bool IsConfirmationEnabled => (IsTokenAuthentication && IsTokenProvided) || (IsCredentialsAuthentication && AreCredentialsProvided);

    private bool IsTokenProvided => IsValueFilled(Token);
    private bool AreCredentialsProvided => IsPasswordProvided && IsUsernameProvided;
    private bool IsUsernameProvided => IsValueFilled(Username);
    private bool IsPasswordProvided => IsValueFilled(Password);
    public string AccountSecurityUrl => Connection.ServerType == ServerType.SonarCloud ? UiResources.SonarCloudAccountSecurityUrl : Path.Combine(Connection.Id, UiResources.SonarQubeAccountSecurityUrl);

    private bool IsValueFilled(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
