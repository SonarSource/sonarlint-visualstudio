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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix.Settings.FileExclusions;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

[ExcludeFromCodeCoverage]
internal sealed partial class SolutionSettingsDialog : Window
{
    private readonly IServiceProvider serviceProvider;
    private readonly ISolutionSettingsStorage solutionSettingsStorage;
    private readonly AnalysisPropertiesControl analysisPropertiesControl;
    private readonly FileExclusionsDialogControl fileExclusionsDialogControl;

    public SolutionSettingsViewModel ViewModel { get; }

    internal SolutionSettingsDialog(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        var solutionUserSettingsUpdater = serviceProvider.GetMefService<ISolutionRawSettingsService>();
        var globalUserSettingsUpdater = serviceProvider.GetMefService<IGlobalRawSettingsService>();
        solutionSettingsStorage = serviceProvider.GetMefService<ISolutionSettingsStorage>();
        var browserService = serviceProvider.GetMefService<IBrowserService>();
        analysisPropertiesControl = new AnalysisPropertiesControl(new AnalysisPropertiesViewModel(solutionUserSettingsUpdater), browserService);
        fileExclusionsDialogControl
            = new FileExclusionsDialogControl(new FileExclusionsViewModel(browserService, globalUserSettingsUpdater, solutionUserSettingsUpdater, FileExclusionScope.Solution),
                themeResponsive: true);
        ViewModel = new SolutionSettingsViewModel(serviceProvider.GetMefService<ISolutionInfoProvider>());
        InitializeComponent();
        AddTabs();
    }

    public void InitializeData()
    {
        analysisPropertiesControl.ViewModel.InitializeAnalysisProperties();
        fileExclusionsDialogControl.ViewModel.InitializeExclusions();
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e) => ApplyAndClose();

    private void ApplyAndClose()
    {
        analysisPropertiesControl.ViewModel.UpdateAnalysisProperties();
        fileExclusionsDialogControl.ViewModel.SaveExclusions();
        Close();
    }

    private void OpenFile(object sender, RequestNavigateEventArgs e)
    {
        solutionSettingsStorage.EnsureSettingsFileExists();
        DocumentOpener.OpenDocumentInVs(serviceProvider, solutionSettingsStorage.SettingsFilePath);
        ApplyAndClose();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// The constructor of the user controls for the tabs are not parameterless, so they cannot be created in XAML directly.
    /// </summary>
    private void AddTabs()
    {
        AnalysisPropertiesTab.Content = analysisPropertiesControl;
        FileExclusionsTab.Content = fileExclusionsDialogControl;
    }
}
