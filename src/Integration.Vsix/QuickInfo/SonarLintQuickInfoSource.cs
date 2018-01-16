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
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                return;
            }

            ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;

            ITextDocument document;
            if (!provider.TextDocumentFactoryService.TryGetTextDocument(subjectBuffer, out document))
            {
                return;
            }

            Func<Sonarlint.Issue, IssueMarker> CreateMarker = issue => issue.ToMarker(currentSnapshot);
            Func<IssueMarker, bool> ContainsTheTriggerPoint = marker => marker.Span.Contains(subjectTriggerPoint.Value);

            var issuesUnderCursor = provider.SonarLintDaemon
                .GetIssues(document.FilePath)
                .Select(CreateMarker)
                .Where(ContainsTheTriggerPoint)
                .ToList();

            if (issuesUnderCursor.Count > 0)
            {
                issuesUnderCursor.ForEach(quickInfoContent.Add);
                applicableToSpan = currentSnapshot.CreateTrackingSpan(issuesUnderCursor[0].Span, SpanTrackingMode.EdgeInclusive);
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
