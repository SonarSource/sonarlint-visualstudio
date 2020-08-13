/*
 * SonarQube Client
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
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.TableControls
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [Name(nameof(VSTableEventProcessorProvider))]
    [Order(Before = Priority.Default)]
    // We can specify multiple data source types and data source i.e. the same provider can be used for
    // multiple table controls/sources
    [DataSourceType(StandardTableDataSources.ErrorTableDataSource)]
    [DataSource(SonarLintTableControlConstants.ErrorListDataSourceIdentifier)]
    internal class VSTableEventProcessorProvider : ITableControlEventProcessorProvider
    {
        private readonly IIssueTablesSelectionMonitor issueTableSelectionMonitor;

        [ImportingConstructor]
        internal VSTableEventProcessorProvider(IIssueTablesSelectionMonitor issueTableSelectionMonitor)
        {
            this.issueTableSelectionMonitor = issueTableSelectionMonitor;
        }

        ITableControlEventProcessor ITableControlEventProcessorProvider.GetAssociatedEventProcessor(IWpfTableControl tableControl)
        {
            if (tableControl == null)
            {
                return null;
            }

            return new EventProcessor(tableControl, issueTableSelectionMonitor);
        }

        /// <summary>
        /// Filters and processes events from a specific WPF table, raising the higher-level "selected issue changed"
        /// changed when appropriate.
        /// </summary>
        internal /* for testing */ class EventProcessor : ITableControlEventProcessor
        {
            private readonly IWpfTableControl wpfTableControl;
            private readonly IIssueTablesSelectionMonitor selectionMonitor;

            public EventProcessor(IWpfTableControl wpfTableControl, IIssueTablesSelectionMonitor errorListSelectionMonitor)
            {
                this.wpfTableControl = wpfTableControl;
                this.selectionMonitor = errorListSelectionMonitor;
            }

            void ITableControlEventProcessor.PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
            {
                try
                {
                    IAnalysisIssue selectedIssue = null;

                    if (wpfTableControl.SelectedEntries.Count() == 1 &&
                        wpfTableControl.SelectedEntry.TryGetSnapshot(out var snapshot, out var index) &&
                        snapshot.TryGetValue(index, SonarLintTableControlConstants.IssueColumnName, out var issueObject) &&
                        issueObject is IAnalysisIssue issueFromTable)
                    {
                        selectedIssue = issueFromTable;
                    }

                    selectionMonitor.SelectionChanged(selectedIssue);
                }
                catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    // No-op
                }
            }

            #region Event callbacks we are not interested in
            void ITableControlEventProcessor.KeyDown(System.Windows.Input.KeyEventArgs args) { /* no-op */ }
            void ITableControlEventProcessor.KeyUp(System.Windows.Input.KeyEventArgs args) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessDragEnter(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessDragLeave(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessDragOver(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessDrop(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessGiveFeedback(ITableEntryHandle entry, System.Windows.GiveFeedbackEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseDown(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseEnter(ITableEntryHandle entry, System.Windows.Input.MouseEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseLeave(ITableEntryHandle entry, System.Windows.Input.MouseEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseLeftButtonDown(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseLeftButtonUp(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseMove(ITableEntryHandle entry, System.Windows.Input.MouseEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseRightButtonDown(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseRightButtonUp(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseUp(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessMouseWheel(ITableEntryHandle entry, System.Windows.Input.MouseWheelEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessNavigateToHelp(ITableEntryHandle entry, TableEntryEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PostprocessQueryContinueDrag(ITableEntryHandle entry, System.Windows.QueryContinueDragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessDragEnter(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessDragLeave(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessDragOver(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessDrop(ITableEntryHandle entry, System.Windows.DragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessGiveFeedback(ITableEntryHandle entry, System.Windows.GiveFeedbackEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseDown(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseEnter(ITableEntryHandle entry, System.Windows.Input.MouseEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseLeave(ITableEntryHandle entry, System.Windows.Input.MouseEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseLeftButtonDown(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseLeftButtonUp(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseMove(ITableEntryHandle entry, System.Windows.Input.MouseEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseRightButtonDown(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseRightButtonUp(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseUp(ITableEntryHandle entry, System.Windows.Input.MouseButtonEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessMouseWheel(ITableEntryHandle entry, System.Windows.Input.MouseWheelEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessNavigateToHelp(ITableEntryHandle entry, TableEntryEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessQueryContinueDrag(ITableEntryHandle entry, System.Windows.QueryContinueDragEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreprocessSelectionChanged(TableSelectionChangedEventArgs e) { /* no-op */ }
            void ITableControlEventProcessor.PreviewKeyDown(System.Windows.Input.KeyEventArgs args) { /* no-op */ }
            void ITableControlEventProcessor.PreviewKeyUp(System.Windows.Input.KeyEventArgs args) { /* no-op */ }
            #endregion
        }
    }
}
