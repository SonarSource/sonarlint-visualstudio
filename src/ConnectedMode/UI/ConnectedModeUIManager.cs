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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

public interface IConnectedModeUIManager
{
    void ShowManageBindingDialog(bool useSharedBindingOnInitialization = false);
}

[Export(typeof(IConnectedModeUIManager))]
[PartCreationPolicy(CreationPolicy.NonShared)]
internal sealed class ConnectedModeUIManager : IConnectedModeUIManager
{
    private readonly IConnectedModeServices connectedModeServices;
    private readonly IConnectedModeBindingServices connectedModeBindingServices;

    [ImportingConstructor]
    public ConnectedModeUIManager(IConnectedModeServices connectedModeServices, IConnectedModeBindingServices connectedModeBindingServices)
    {
        this.connectedModeServices = connectedModeServices;
        this.connectedModeBindingServices = connectedModeBindingServices;
    }

    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public void ShowManageBindingDialog(bool useSharedBindingOnInitialization = false)
    {
        var manageBindingDialog = new ManageBindingDialog(connectedModeServices, connectedModeBindingServices, useSharedBindingOnInitialization);
        manageBindingDialog.ShowDialog(Application.Current.MainWindow);
    }
}