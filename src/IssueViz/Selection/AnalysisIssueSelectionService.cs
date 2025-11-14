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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.IssueVisualization.Selection;

[Export(typeof(IAnalysisIssueSelectionService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class AnalysisIssueSelectionService : IAnalysisIssueSelectionService
{
    private Guid uiContextGuid = new Guid(Commands.Constants.UIContextGuid);
    private readonly IVsUIServiceOperation uiServiceOperation;
    private readonly IIssueSelectionService selectionService;

    private IAnalysisIssueVisualization selectedIssue;
    private IAnalysisIssueFlowVisualization selectedFlow;
    private IAnalysisIssueLocationVisualization selectedLocation;
    private bool disposed = false;

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

    [ImportingConstructor]
    public AnalysisIssueSelectionService(
        IVsUIServiceOperation uiServiceOperation,
        IIssueSelectionService selectionService)
    {
        this.uiServiceOperation = uiServiceOperation;
        this.selectionService = selectionService;
        this.selectionService.SelectedIssueChanged += SelectionService_SelectedIssueChanged;
    }

    public IAnalysisIssueVisualization SelectedIssue
    {
        get => selectedIssue;
        set
        {
            selectedIssue = value;
            selectedFlow = GetFirstFlowOrDefault();
            selectedLocation = GetFirstLocationOrDefault();

            UpdateUiContext();

            RaiseSelectionChanged(SelectionChangeLevel.Issue);
        }
    }

    public IAnalysisIssueFlowVisualization SelectedFlow
    {
        get => selectedFlow;
        set
        {
            selectedFlow = value;
            selectedLocation = GetFirstLocationOrDefault();

            RaiseSelectionChanged(SelectionChangeLevel.Flow);
        }
    }

    public IAnalysisIssueLocationVisualization SelectedLocation
    {
        get => selectedLocation;
        set
        {
            selectedLocation = value;

            RaiseSelectionChanged(SelectionChangeLevel.Location);
        }
    }

    private void RaiseSelectionChanged(SelectionChangeLevel changeLevel) =>
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(changeLevel, SelectedIssue, SelectedFlow, SelectedLocation));

    private void UpdateUiContext() =>
        uiServiceOperation.Execute<Microsoft.VisualStudio.Shell.Interop.SVsShellMonitorSelection, Microsoft.VisualStudio.Shell.Interop.IVsMonitorSelection>(monitorSelection =>
        {
            if (monitorSelection.GetCmdUIContextCookie(ref uiContextGuid, out uint cookie) == Microsoft.VisualStudio.VSConstants.S_OK)
            {
                var shouldActivate = SelectedIssue != null;
                monitorSelection.SetCmdUIContext(cookie, shouldActivate ? 1 : 0);
            }
        });

    private IAnalysisIssueFlowVisualization GetFirstFlowOrDefault() =>
        selectedIssue?.Flows?.FirstOrDefault();

    private IAnalysisIssueLocationVisualization GetFirstLocationOrDefault() =>
        selectedFlow?.Locations?.FirstOrDefault();

    private void SelectionService_SelectedIssueChanged(object sender, EventArgs e)
    {
        if (selectionService.SelectedIssue != SelectedIssue)
        {
            var hasSecondaryLocations = selectionService.SelectedIssue != null &&
                                        selectionService.SelectedIssue.Flows.SelectMany(x => x.Locations).Any();

            SelectedIssue = hasSecondaryLocations ? selectionService.SelectedIssue : null;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        SelectionChanged = null;
        selectionService.SelectedIssueChanged -= SelectionService_SelectedIssueChanged;
    }
}
