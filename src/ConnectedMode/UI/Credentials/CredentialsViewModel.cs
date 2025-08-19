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

using System.IO;
using System.Security;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials;

public class CredentialsViewModel(ConnectionInfo connectionInfo, ISlCoreConnectionAdapter slCoreConnectionAdapter, IProgressReporterViewModel progressReporterViewModel) : ViewModelBase
{
    public const string SecurityPageUrl = "account/security";
    private SecureString token = new();
    private CancellationTokenSource tokenGenerationCancellationSource;

    public ConnectionInfo ConnectionInfo { get; } = connectionInfo;
    public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;

    public CancellationTokenSource TokenGenerationCancellationSource
    {
        get => tokenGenerationCancellationSource;
        private set
        {
            CancelAndDisposeCancellationToken(tokenGenerationCancellationSource);
            tokenGenerationCancellationSource = value;
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

    public bool ShouldTokenBeFilled => !IsTokenProvided;
    public bool IsConfirmationEnabled => !ProgressReporterViewModel.IsOperationInProgress && IsTokenProvided;

    private bool IsTokenProvided => IsSecureStringFilled(Token);
    public string AccountSecurityUrl =>
        ConnectionInfo.ServerType == ConnectionServerType.SonarCloud
            ? Path.Combine(ConnectionInfo.CloudServerRegion.Url.ToString(), SecurityPageUrl)
            : Path.Combine(ConnectionInfo.Id, SecurityPageUrl);

    private static bool IsSecureStringFilled(SecureString secureString) => !string.IsNullOrWhiteSpace(secureString?.ToUnsecureString());

    public ICredentialsModel GetCredentialsModel() => new TokenCredentialsModel(Token);

    internal async Task<bool> ValidateConnectionAsync()
    {
        var validationParams = new TaskToPerformParams<ResponseStatus>(AdapterValidateConnectionAsync, UiResources.ValidatingConnectionProgressText, UiResources.ValidatingConnectionFailedText)
        {
            AfterProgressUpdated = AfterProgressStatusUpdated
        };
        var adapterResponse = await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        return adapterResponse.Success;
    }

    internal async Task<ResponseStatus> AdapterValidateConnectionAsync() => await slCoreConnectionAdapter.ValidateConnectionAsync(ConnectionInfo, GetCredentialsModel());

    internal async Task<ResponseStatusWithData<string>> GenerateTokenWithProgressAsync()
    {
        var validationParams = new TaskToPerformParams<ResponseStatusWithData<string>>(GenerateTokenAsync, UiResources.GeneratingTokenProgressText, UiResources.GeneratingTokenFailedText)
        {
            AfterProgressUpdated = AfterProgressStatusUpdated
        };
        var adapterResponse = await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        return adapterResponse;
    }

    internal async Task<ResponseStatusWithData<string>> GenerateTokenAsync()
    {
        TokenGenerationCancellationSource = new CancellationTokenSource();
        return await slCoreConnectionAdapter.GenerateTokenAsync(ConnectionInfo, TokenGenerationCancellationSource.Token);
    }

    internal void AfterProgressStatusUpdated() => RaisePropertyChanged(nameof(IsConfirmationEnabled));

    internal static void CancelAndDisposeCancellationToken(CancellationTokenSource cancellationTokenSource)
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
