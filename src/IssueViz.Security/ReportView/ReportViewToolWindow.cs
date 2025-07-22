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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

[Guid(ToolWindowIdAsString)]
[ExcludeFromCodeCoverage]
internal class ReportViewToolWindow : ToolWindowPane
{
    private const string ToolWindowIdAsString = "6CDF14D1-EFE5-4651-B8B2-32D7AE954E16";
    public static readonly Guid ToolWindowId = new Guid(ToolWindowIdAsString);

    public ReportViewToolWindow(IServiceProvider serviceProvider)
    {
        var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
        var activeSolutionBoundTracker = componentModel?.GetService<IActiveSolutionBoundTracker>();
        var browserService = componentModel?.GetService<IBrowserService>();
        var dependencyRiskStore = componentModel?.GetService<IDependencyRisksStore>();
        var threadHandling = componentModel?.GetService<IThreadHandling>();
        Caption = Resources.ReportViewToolWindowCaption;
        Content = new ReportViewControl(activeSolutionBoundTracker, browserService, dependencyRiskStore, threadHandling);
    }
}
