/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Navigation;
using SonarLint.VisualStudio.Core.Telemetry;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public partial class ServerSelectionDialog : Window
{
    private readonly IConnectedModeUIServices connectedModeUiServices;
    private readonly ITelemetryManager telemetryManager;

    public ServerSelectionViewModel ViewModel { get; }

    public ServerSelectionDialog(IConnectedModeUIServices connectedModeUiServices, ITelemetryManager telemetryManager)
    {
        this.connectedModeUiServices = connectedModeUiServices;
        this.telemetryManager = telemetryManager;
        ViewModel = new ServerSelectionViewModel(connectedModeUiServices);
        InitializeComponent();
    }

    private void FreeSonaQubeCloudFreeTier_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        connectedModeUiServices.BrowserService.Navigate(TelemetryLinks.SonarQubeCloudFreeSignUpCreateNewConnection.GetUtmLink);
        telemetryManager.LinkClicked(TelemetryLinks.SonarQubeCloudFreeSignUpCreateNewConnection.Id);
    }

    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
