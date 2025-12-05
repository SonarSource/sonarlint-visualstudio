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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Selection;

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
        Caption = Resources.ReportViewToolWindowCaption;
        Content = new ReportViewControl(
            componentModel?.GetService<IActiveSolutionBoundTracker>(),
            componentModel?.GetService<IBrowserService>(),
            componentModel?.GetService<IHotspotsReportViewModel>(),
            componentModel?.GetService<IDependencyRisksReportViewModel>(),
            componentModel?.GetService<ITaintsReportViewModel>(),
            componentModel?.GetService<IIssuesReportViewModel>(),
            componentModel?.GetService<INavigateToRuleDescriptionCommand>(),
            componentModel?.GetService<ILocationNavigator>(),
            componentModel?.GetService<ITelemetryManager>(),
            componentModel?.GetService<IIssueSelectionService>(),
            componentModel?.GetService<IActiveDocumentLocator>(),
            componentModel?.GetService<IActiveDocumentTracker>(),
            componentModel?.GetService<IDocumentTracker>(),
            componentModel?.GetService<IThreadHandling>(),
            componentModel?.GetService<IInitializationProcessorFactory>(),
            componentModel?.GetService<IFocusOnNewCodeServiceUpdater>());
    }
}
