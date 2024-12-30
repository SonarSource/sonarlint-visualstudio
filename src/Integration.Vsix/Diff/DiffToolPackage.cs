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


using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Differencing;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

namespace SonarLint.VisualStudio.Integration.Vsix.Diff
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideToolWindow(typeof(DiffToolWindowPane))]
    [Guid(PackageGuidString)]
    public sealed class DiffToolPackage : AsyncPackage
    {
        public const string PackageGuidString = "B840378A-D89A-4F1C-8955-97DD06076E56";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await DiffToolWindowCommand.InitializeAsync(this);
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(DiffToolWindowPane) && GetService(typeof(SComponentModel)) is IComponentModel componentModel)
            {
                var differenceBufferFactoryService = componentModel.GetService<IDifferenceBufferFactoryService>();
                var differenceViewerFactoryService = componentModel.GetService<IWpfDifferenceViewerFactoryService>();

                return new DiffToolWindowPane(differenceBufferFactoryService, differenceViewerFactoryService);
            }

            return base.InstantiateToolWindow(toolWindowType);
        }
    }
}
