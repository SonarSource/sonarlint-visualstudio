﻿/*
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
using System.Windows.Controls;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.UI;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings.FileExclusions;

/// <summary>
///     Interaction logic for FileExclusionsDialogControl.xaml
/// </summary>
[ExcludeFromCodeCoverage]
internal partial class FileExclusionsDialogControl : UserControl
{
    public FileExclusionsViewModel ViewModel { get; }

    internal FileExclusionsDialogControl(FileExclusionsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void ViewInBrowser(object sender, RequestNavigateEventArgs args) => ViewModel.ViewInBrowser(args.Uri.AbsoluteUri);

    private void Add_OnClick(object sender, RoutedEventArgs e)
    {
        var addExclusionWindow = new AddExclusionDialog();
        if (addExclusionWindow.ShowDialog(Application.Current.MainWindow) == true)
        {
            ViewModel.AddExclusion(addExclusionWindow.ViewModel.Pattern);
        }
    }

    private void Edit_OnClick(object sender, RoutedEventArgs e)
    {
        var addExclusionWindow = new EditExclusionDialog(ViewModel.SelectedExclusion.Pattern);
        if (addExclusionWindow.ShowDialog(Application.Current.MainWindow) == true)
        {
            ViewModel.SelectedExclusion.Pattern = addExclusionWindow.ViewModel.Pattern;
        }
    }

    private void Delete_OnClick(object sender, RoutedEventArgs e) => ViewModel.RemoveExclusion();
}
