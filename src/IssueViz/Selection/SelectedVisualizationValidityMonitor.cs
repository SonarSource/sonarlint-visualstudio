using System;
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;

namespace SonarLint.VisualStudio.IssueVisualization.Selection
{
    internal interface ISelectedVisualizationValidityMonitor : IDisposable
    {
    }

    [Export(typeof(ISelectedVisualizationValidityMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SelectedVisualizationValidityMonitor : ISelectedVisualizationValidityMonitor
    {
        private readonly IAnalysisIssueSelectionService selectionService;
        private readonly IIssueLocationStoreAggregator locationStoreAggregator;

        [ImportingConstructor]
        public SelectedVisualizationValidityMonitor(IAnalysisIssueSelectionService selectionService, IIssueLocationStoreAggregator locationStoreAggregator)
        {
            this.selectionService = selectionService;
            this.locationStoreAggregator = locationStoreAggregator;

            locationStoreAggregator.IssuesChanged += LocationStoreAggregator_IssuesChanged;
        }

        private void LocationStoreAggregator_IssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            if (selectionService.SelectedIssue == null)
            {
                return;
            }

            var issuesChangedInSelectedFile = e.AnalyzedFiles.Any(x => PathHelper.IsMatchingPath(x, selectionService.SelectedIssue.CurrentFilePath));

            if (issuesChangedInSelectedFile)
            {
                var selectedIssueNoLongerExists = !locationStoreAggregator.Contains(selectionService.SelectedIssue);

                if (selectedIssueNoLongerExists)
                {
                    selectionService.SelectedIssue = null;
                }
            }
        }

        public void Dispose()
        {
            locationStoreAggregator.IssuesChanged -= LocationStoreAggregator_IssuesChanged;
        }
    }
}
