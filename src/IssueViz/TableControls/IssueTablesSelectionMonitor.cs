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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.IssueVisualization.TableControls
{
    /// <summary>
    /// Singleton that processes "selected issue changed" events from multiple different
    /// table sources
    /// </summary>
    [Export(typeof(IssueTablesSelectionMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class IssueTablesSelectionMonitor : IIssueTablesSelectionMonitor
    {
        private readonly ILogger logger;
        private readonly ISet<IIssueTableEventSource> eventSources;

        [ImportingConstructor]
        public IssueTablesSelectionMonitor(ILogger logger)
        {
            this.logger = logger;
            eventSources = new HashSet<IIssueTableEventSource>();
        }

        void IIssueTablesSelectionMonitor.AddEventSource(IIssueTableEventSource source)
        {
            if (source == null)
            {
                return;
            }

            lock(eventSources)
            {
                eventSources.Add(source);
                source.SelectedIssueChanged += OnSelectedIssueChanged;
            }
        }

        private void OnSelectedIssueChanged(object sender, IssueTableSelectionChangedEventArgs e)
        {
            // TODO - process the selection changed event.
            // For now, dump the output
            if (e.SelectedIssue == null)
            {
                logger.WriteLine("OnSelectedIssueChanged: null");
            }
            else
            {
                logger.WriteLine($"OnSelectedIssueChanged: RuleKey: {e.SelectedIssue.RuleKey}, Position: [{e.SelectedIssue.StartLine}, {e.SelectedIssue.StartLineOffset}]->[{e.SelectedIssue.EndLine}, {e.SelectedIssue.EndLineOffset}] File: {e.SelectedIssue.FilePath}");
            }
        }
    }
}
