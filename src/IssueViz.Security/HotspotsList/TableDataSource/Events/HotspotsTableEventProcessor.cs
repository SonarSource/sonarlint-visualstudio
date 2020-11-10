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

using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.Events
{
    internal class HotspotsTableEventProcessor : ITableControlEventProcessor
    {
        private readonly IWpfTableControl tableControl;
        private readonly ILocationNavigator locationNavigator;

        public HotspotsTableEventProcessor(IWpfTableControl tableControl, ILocationNavigator locationNavigator)
        {
            this.tableControl = tableControl;
            this.locationNavigator = locationNavigator;
        }

        void ITableControlEventProcessor.KeyDown(KeyEventArgs args)
        {
            if (args.Key == Key.Enter)
            {
                TryNavigateToHotspot(tableControl.SelectedEntry);
            }
        }

        void ITableControlEventProcessor.PostprocessMouseDown(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                TryNavigateToHotspot(entry);
            }
        }

        private void TryNavigateToHotspot(ITableEntry entry)
        {
            if (!(entry?.Identity is IAnalysisIssueVisualization issueVisualization))
            {
                return;
            }

            locationNavigator.TryNavigate(issueVisualization);
        }

        #region ITableControlEventProcessor unimplemented methods

        void ITableControlEventProcessor.PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
        }

        void ITableControlEventProcessor.KeyUp(KeyEventArgs args)
        {
        }

        void ITableControlEventProcessor.PostprocessDragEnter(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessDragLeave(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessDragOver(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessDrop(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessGiveFeedback(ITableEntryHandle entry, GiveFeedbackEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseEnter(ITableEntryHandle entry, MouseEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseLeave(ITableEntryHandle entry, MouseEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseLeftButtonDown(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseLeftButtonUp(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseMove(ITableEntryHandle entry, MouseEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseRightButtonDown(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseRightButtonUp(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseUp(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessMouseWheel(ITableEntryHandle entry, MouseWheelEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessNavigateToHelp(ITableEntryHandle entry, TableEntryEventArgs e)
        {
        }

        void ITableControlEventProcessor.PostprocessQueryContinueDrag(ITableEntryHandle entry, QueryContinueDragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessDragEnter(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessDragLeave(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessDragOver(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessDrop(ITableEntryHandle entry, DragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessGiveFeedback(ITableEntryHandle entry, GiveFeedbackEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseDown(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseEnter(ITableEntryHandle entry, MouseEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseLeave(ITableEntryHandle entry, MouseEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseLeftButtonDown(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseLeftButtonUp(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseMove(ITableEntryHandle entry, MouseEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseRightButtonDown(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseRightButtonUp(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseUp(ITableEntryHandle entry, MouseButtonEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessMouseWheel(ITableEntryHandle entry, MouseWheelEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessNavigateToHelp(ITableEntryHandle entry, TableEntryEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessQueryContinueDrag(ITableEntryHandle entry, QueryContinueDragEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
        }

        void ITableControlEventProcessor.PreviewKeyDown(KeyEventArgs args)
        {
        }

        void ITableControlEventProcessor.PreviewKeyUp(KeyEventArgs args)
        {
        }

        #endregion
    }
}
