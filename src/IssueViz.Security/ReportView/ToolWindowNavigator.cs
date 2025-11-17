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

using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

[ExcludeFromCodeCoverage]
public class ToolWindowNavigator
{
    private readonly IMenuCommandService commandService;
    public static ToolWindowNavigator Instance { get; private set; }

    private ToolWindowNavigator(IMenuCommandService commandService)
    {
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
    }

    internal static async Task CreateAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;

        Instance = new ToolWindowNavigator(commandService);
    }

    internal void ShowIssueVisualizationToolWindow()
    {
        var commandId = new CommandID(Constants.CommandSetGuid, Constants.ViewToolWindowCommandId);
        commandService.GlobalInvoke(commandId);
    }
}
