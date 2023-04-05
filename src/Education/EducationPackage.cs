/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Commands;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Education
{
    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("7ef4a2de-4035-48c3-b273-4195d0f1186b")]
    [ProvideMenuResource("Menus.ctmenu", 2)]
    [ProvideToolWindow(typeof(RuleHelpToolWindow), MultiInstances = false, Transient = true, Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.SolutionExplorer, Width = 325, Height = 400)]
    public sealed class EducationPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await RuleHelpWindowCommand.InitializeAsync(this);
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(RuleHelpToolWindow))
            {
                var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
                var browserService = componentModel.GetService<IBrowserService>();
                var education = componentModel.GetService<IEducation>();
                return new RuleHelpToolWindow(browserService, education);
            }

            return base.InstantiateToolWindow(toolWindowType);
        }
    }
}
