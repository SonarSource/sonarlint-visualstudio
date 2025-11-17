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

using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal abstract class ServerViewModel : ViewModelBase, IDisposable, IRequireInitialization
{
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private bool isCloud;
    private bool isConnectedMode;

    public bool IsCloud
    {
        get => isCloud;
        private set
        {
            isCloud = value;
            RaisePropertyChanged();
        }
    }

    public bool IsConnectedMode
    {
        get => isConnectedMode;
        private set
        {
            isConnectedMode = value;
            RaisePropertyChanged();
        }
    }

    public IInitializationProcessor InitializationProcessor { get; }

    protected ServerViewModel(IActiveSolutionBoundTracker activeSolutionBoundTracker, IInitializationProcessorFactory initializationProcessorFactory)
    {
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;

        InitializationProcessor = initializationProcessorFactory.CreateAndStart<ServerViewModel>([activeSolutionBoundTracker], () =>
        {
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
            UpdateConnectedModeState(activeSolutionBoundTracker.CurrentConfiguration);
        });
    }

    protected abstract void HandleBindingChange(BindingConfiguration newBinding);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }
        activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
    }

    private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs args)
    {
        UpdateConnectedModeState(args.Configuration);
        HandleBindingChange(args.Configuration);
    }

    private void UpdateConnectedModeState(BindingConfiguration bindingConfiguration)
    {
        IsConnectedMode = bindingConfiguration.Mode.IsInAConnectedMode();
        IsCloud = IsCurrentConfigurationToCloud(bindingConfiguration);
    }

    private static bool IsCurrentConfigurationToCloud(BindingConfiguration bindingConfiguration) => bindingConfiguration?.Project?.ServerConnection is ServerConnection.SonarCloud;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
