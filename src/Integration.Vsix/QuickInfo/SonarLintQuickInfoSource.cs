/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public sealed class SonarLintQuickInfoSource : IQuickInfoSource
    {
        private readonly ITextBuffer subjectBuffer;
        private readonly SonarLintQuickInfoSourceProvider provider;
        private bool disposed;

        public SonarLintQuickInfoSource(SonarLintQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            this.provider = provider;
            this.subjectBuffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent,
            out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            // Map the trigger point down to our buffer.
            var triggerPoint = session.GetTriggerPoint(subjectBuffer.CurrentSnapshot);
            if (triggerPoint == null)
            {
                return;
            }

            ITextDocument document;
            if (!provider.TextDocumentFactoryService.TryGetTextDocument(subjectBuffer, out document))
            {
                return;
            }

            ITextSnapshot currentSnapshot = triggerPoint.Value.Snapshot;

            Func<Sonarlint.Issue, IssueMarker> CreateMarker = issue => issue.ToMarker(currentSnapshot);
            Func<IssueMarker, bool> ContainsTheTriggerPoint = marker => marker.Span.Contains(triggerPoint.Value);

            var issueMarkers = provider.SonarLintDaemon
                .GetIssues(document.FilePath)
                .Select(CreateMarker)
                .Where(ContainsTheTriggerPoint)
                .ToList();

            if (issueMarkers.Count > 0)
            {
                issueMarkers.ForEach(issue => quickInfoContent.Add(issue.Issue.Message));
                applicableToSpan = currentSnapshot.CreateTrackingSpan(issueMarkers[0].Span, SpanTrackingMode.EdgeInclusive);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }
    }
}
