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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Differencing;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView
{
    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideToolWindow(typeof(DiffViewToolWindowPane))]
    [Guid(PackageGuidString)]
    public sealed class DiffViewToolPackage : AsyncPackage
    {
        public const string PackageGuidString = "DD11524E-BB8A-4BFF-B693-949686EBD9E3";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) =>
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType != typeof(DiffViewToolWindowPane) || GetService(typeof(SComponentModel)) is not IComponentModel componentModel)
            {
                return base.InstantiateToolWindow(toolWindowType);
            }

            var differenceBufferFactoryService = componentModel.GetService<IDifferenceBufferFactoryService>();
            var differenceViewerFactoryService = componentModel.GetService<IWpfDifferenceViewerFactoryService>();

            return new DiffViewToolWindowPane(differenceBufferFactoryService, differenceViewerFactoryService);
        }
    }
}
