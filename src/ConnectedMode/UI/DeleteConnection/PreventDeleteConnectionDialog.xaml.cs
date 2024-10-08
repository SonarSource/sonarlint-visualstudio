﻿/*
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
using System.Windows.Navigation;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UI.DeleteConnection;

[ExcludeFromCodeCoverage]
public partial class PreventDeleteConnectionDialog : Window
{
    private readonly IBrowserService browserService;
    public PreventDeleteConnectionViewModel ViewModel { get; }
    
    public PreventDeleteConnectionDialog(IReadOnlyList<string> projectsToUnbind, ConnectionInfo connectionInfo, IBrowserService browserService)
    {
        this.browserService = browserService;
        ViewModel = new PreventDeleteConnectionViewModel(projectsToUnbind, connectionInfo);
        InitializeComponent();
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void ViewWebsite(object sender, RequestNavigateEventArgs e)
    {
        browserService.Navigate(e.Uri.AbsoluteUri);
    }
}

