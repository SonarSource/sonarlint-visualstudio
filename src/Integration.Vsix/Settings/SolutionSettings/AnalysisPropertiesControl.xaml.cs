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
using System.Windows.Controls;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

/// <summary>
/// Interaction logic for AnalysisSettingsControl.xaml
/// </summary>
[ExcludeFromCodeCoverage]
internal partial class AnalysisPropertiesControl : UserControl
{
    private readonly IBrowserService browserService;
    public AnalysisPropertiesViewModel ViewModel { get; }

    internal AnalysisPropertiesControl(AnalysisPropertiesViewModel viewModel, IBrowserService browserService)
    {
        this.browserService = browserService;
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void Add_OnClick(object sender, RoutedEventArgs e)
    {
        var addPropertyWindow = new AddAnalysisPropertyDialog();
        if (addPropertyWindow.ShowDialog(GetParentWindow()) == true)
        {
            ViewModel.AddProperty(addPropertyWindow.ViewModel.Name, addPropertyWindow.ViewModel.Value);
        }
    }

    private Window GetParentWindow()
    {
        var parent = Parent;
        while (parent is FrameworkElement frameworkElement)
        {
            if (frameworkElement.Parent is Window parentWindow)
            {
                return parentWindow;
            }
            parent = frameworkElement.Parent;
        }
        return Application.Current.MainWindow;
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e) => ViewModel.RemoveSelectedProperty();

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e) => browserService.Navigate(e.Uri.AbsoluteUri);
}
