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
using System.Windows;
using System.Windows.Navigation;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

[ExcludeFromCodeCoverage]
internal sealed partial class SolutionSettingsDialog : Window
{
    private readonly IServiceProvider serviceProvider;
    private readonly IUserSettingsProvider userSettingsProvider;

    internal SolutionSettingsDialog(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        userSettingsProvider = serviceProvider.GetMefService<IUserSettingsProvider>();
        InitializeComponent();
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e) => ApplyAndClose();

    private void ApplyAndClose()
    {
        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2100: save to settings.json file
        Close();
    }

    private void OpenFile(object sender, RequestNavigateEventArgs e)
    {
        OpenDocumentService.OpenDocumentInVs(serviceProvider, userSettingsProvider.SettingsFilePath);
        ApplyAndClose();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
}
