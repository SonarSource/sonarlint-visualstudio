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

using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

namespace SonarLint.VisualStudio.Integration.Vsix.Diff
{
    internal sealed class DiffToolWindowCommand
    {
        private const int CommandId = 0x100;
        internal static readonly Guid CommandSet = new Guid("80127033-1819-4996-8C45-E9C96F75E2A7");

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand((s, e) => Execute(package), menuCommandId);
            commandService?.AddCommand(menuItem);
        }

        private static void Execute(AsyncPackage package)
        {
            package.JoinableTaskFactory.RunAsync(async () =>
            {
                var window = await package.ShowToolWindowAsync(typeof(DiffToolWindow), 0, true, package.DisposalToken);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException($"Cannot create {nameof(DiffToolWindow)}");
                }
            });
        }
    }
}
