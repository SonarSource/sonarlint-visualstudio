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
    internal sealed class SonarLintQuickInfoSource : IQuickInfoSource
    {
        private readonly ITextSnapshot currentSnapshot;
        private readonly SonarLintQuickInfoSourceProvider provider;
        private readonly string filePath;
        private bool disposed;

        public SonarLintQuickInfoSource(SonarLintQuickInfoSourceProvider provider, ITextSnapshot currentSnapshot,
            string filePath)
        {
            this.provider = provider;
            this.currentSnapshot = currentSnapshot;
            this.filePath = filePath;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent,
            out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            var issueMarkers = GetIssueMarkers(session);

            foreach (var marker in issueMarkers)
            {
                quickInfoContent.Add(marker.Issue.Message);
                applicableToSpan = currentSnapshot.CreateTrackingSpan(marker.Span, SpanTrackingMode.EdgeInclusive);
            }
        }

        private IEnumerable<IssueMarker> GetIssueMarkers(IQuickInfoSession session)
        {
            // Map the trigger point down to our buffer.
            var triggerPoint = session.GetTriggerPoint(currentSnapshot);
            if (triggerPoint == null)
            {
                return Enumerable.Empty<IssueMarker>();
            }

            Func<IssueMarker, bool> ContainsTheTriggerPoint = marker => marker.Span.Contains(triggerPoint.Value);

            return provider.SonarLintDaemon
                .GetIssues(filePath)
                .Select(CreateMarker)
                .Where(ContainsTheTriggerPoint);
        }

        private IssueMarker CreateMarker(Sonarlint.Issue issue) =>
            issue.ToMarker(currentSnapshot);

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
