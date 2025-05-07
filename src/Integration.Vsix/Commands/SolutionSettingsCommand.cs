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

using System.Windows;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal class SolutionSettingsCommand : VsCommandBase
{
    private readonly IServiceProvider serviceProvider;
    internal const int Id = 0x1028;

    internal SolutionSettingsCommand(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    protected override void InvokeInternal()
    {
        var solutionSettingsWindow = new SolutionSettingsDialog(serviceProvider) { Owner = Application.Current.MainWindow };
        solutionSettingsWindow.ShowDialog();
    }

    protected override void QueryStatusInternal(OleMenuCommand command)
    {
        var solutionInfoProvider = serviceProvider.GetMefService<ISolutionInfoProvider>();
        command.Enabled = solutionInfoProvider?.GetSolutionName() != null;
    }
}
