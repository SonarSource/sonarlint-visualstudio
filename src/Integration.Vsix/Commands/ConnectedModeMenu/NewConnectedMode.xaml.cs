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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.DeleteConnection;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;
using SonarLint.VisualStudio.Core;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public sealed partial class NewConnectedMode : DialogWindow
    {
        private readonly IBrowserService browserService;

        internal NewConnectedMode(IBrowserService browserService)
        {
            this.browserService = browserService;
            this.InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            new ServerSelectionDialog(browserService).ShowDialog();
        }

        private void Credentials_OnClick(object sender, RoutedEventArgs e)
        {
            new CredentialsDialog(browserService, new ConnectionInfo.Connection("http://localhost:9000", ServerType.SonarQube, true), withNextButton:true).ShowDialog();
        }

        private void Connections_OnClick(object sender, RoutedEventArgs e)
        {
            new ManageConnectionsDialog(browserService).ShowDialog();
        }

        private void ShowOrganizationSelectionDialog(object sender, RoutedEventArgs e)
        {
            var organizationSelectionDialog = new OrganizationSelectionDialog(
            [
                new("mykey1", "totallydifferentname"),
                new("myFAVOURITEkey", "maybe_a_differentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
                new("mykey1", "totallydifferentname"),
            ]);
            organizationSelectionDialog.ShowDialog();
        }

        private void ProjectSelectionButton_OnClick(object sender, RoutedEventArgs e)
        {
            new ProjectSelectionDialog(new ConnectionInfo.Connection("http://localhost:9000", ServerType.SonarQube, true)).ShowDialog();
        }

        private void DeleteConnection_OnClick(object sender, RoutedEventArgs e)
        {
            new DeleteConnectionDialog(
                    [
                        "A",
                        "A",
                        "B",
                        "B",
                        "B",
                        "B",
                        "C",
                        "C",
                        "C",
                        "C",
                        "D",
                        "D",
                        "D",
                        "D",
                        "D",
                    ],
                    new ConnectionInfo.Connection("https://next.sonarqube.com/next", ServerType.SonarQube, true))
                .ShowDialog();
        }

        private void DeleteEmptyConnection_OnClick(object sender, RoutedEventArgs e)
        {
            new DeleteConnectionDialog(null, new ConnectionInfo.Connection("SonarSource", ServerType.SonarCloud, false)).ShowDialog();
        }

        private void ManageBindingDialog_OnClick(object sender, RoutedEventArgs e)
        {
            new ManageBindingDialog(browserService, new SolutionInfoModel("VS Sample 2022", SolutionType.Solution)).ShowDialog();
        }
    }
}
