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
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal sealed class SonarLintQuickInfoSource : IQuickInfoSource
    {
        private readonly ITextBuffer subjectBuffer;
        private readonly SonarLintQuickInfoSourceProvider provider;
        private readonly IIssueConverter issueConverter;
        private readonly string filePath;

        public SonarLintQuickInfoSource(SonarLintQuickInfoSourceProvider provider, ITextBuffer subjectBuffer, string filePath)
            : this(provider, subjectBuffer, filePath, new IssueConverter()) { }

        public SonarLintQuickInfoSource(SonarLintQuickInfoSourceProvider provider, ITextBuffer subjectBuffer,
            string filePath, IIssueConverter issueConverter)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            if (subjectBuffer == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (issueConverter == null)
            {
                throw new ArgumentNullException(nameof(issueConverter));
            }

            this.provider = provider;
            this.subjectBuffer = subjectBuffer;
            this.filePath = filePath;
            this.issueConverter = issueConverter;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent,
            out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            var issueMarkers = GetIssueMarkers(session);

            foreach (var marker in issueMarkers)
            {
                quickInfoContent.Add(marker.Issue.Message);
                applicableToSpan = subjectBuffer.CurrentSnapshot
                    .CreateTrackingSpan(marker.Span, SpanTrackingMode.EdgeInclusive);
            }
        }

        private IEnumerable<IssueMarker> GetIssueMarkers(IQuickInfoSession session)
        {
            // Map the trigger point down to our buffer.
            var triggerPoint = session.GetTriggerPoint(subjectBuffer.CurrentSnapshot);
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
            issueConverter.ToMarker(issue, subjectBuffer.CurrentSnapshot);

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
