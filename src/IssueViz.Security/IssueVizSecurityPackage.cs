/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Security.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security
{
    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("D7D54E08-45E1-49A6-AA53-AF1CFAA6EBDC")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(HotspotsToolWindow), MultiInstances = false, Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.SolutionExplorer, Width = 325, Height = 400)]
    public sealed class IssueVizSecurityPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await HotspotsToolWindowCommand.InitializeAsync(this);
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(HotspotsToolWindow))
            {
                TestShellLoad();

                return new HotspotsToolWindow();
            }

            return base.InstantiateToolWindow(toolWindowType);
        }

        private void TestShellLoad()
        {
            var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
            var tableManagerProvider = componentModel.GetService<ITableManagerProvider>();
            var wpfTableControlProvider = componentModel.GetService<IWpfTableControlProvider>();
            var tableManager = tableManagerProvider.GetTableManager(nameof(IssueVizSecurityPackage));
            var tableControl = wpfTableControlProvider.CreateControl(tableManager,
                true,
                new[]
                {
                    new ColumnState(StandardTableKeyNames.DocumentName, true, 200),
                    new ColumnState(StandardTableKeyNames.Line, true, 200)
                }, StandardTableKeyNames.DocumentName, StandardTableKeyNames.Line);
        }
    }
}
