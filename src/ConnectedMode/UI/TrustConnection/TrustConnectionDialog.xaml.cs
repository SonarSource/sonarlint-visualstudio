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

using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Windows;
using System.Windows.Navigation;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UI.TrustConnection;

[ExcludeFromCodeCoverage]
public partial class TrustConnectionDialog : Window
{
    private readonly IConnectedModeServices connectedModeServices;

    public ConnectionInfo ConnectionInfo { get; }

    public TrustConnectionDialog(IConnectedModeServices connectedModeServices, ServerConnection serverConnection, SecureString token)
    {
        this.connectedModeServices = connectedModeServices;
        ConnectionInfo = serverConnection.ToConnection().Info;
        InitializeComponent();
    }

    private void ViewWebsite(object sender, RequestNavigateEventArgs e) => connectedModeServices.BrowserService.Navigate(e.Uri.AbsoluteUri);

    private void TrustServerButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
