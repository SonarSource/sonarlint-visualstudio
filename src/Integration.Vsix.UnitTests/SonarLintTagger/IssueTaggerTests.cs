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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class IssueTaggerTests
    {
        private static readonly IEnumerable<IssueMarker> ValidMarkerList = new[] { CreateValidMarker() };
        private static readonly ITextSnapshot ValidTextSnapshot = Mock.Of<ITextSnapshot>();
        private static readonly SnapshotSpan ValidSnapshotSpan = new SnapshotSpan(ValidTextSnapshot, 0, 0);

        [TestMethod]
        public void UpdateMarkers_NoSpan_EventNotRaised()
        {
            var testSubject = new IssueTagger(null, null);

            var eventRaised = false;
            testSubject.TagsChanged += (sender, e) => eventRaised = true;

            testSubject.UpdateMarkers(ValidMarkerList, null);

            eventRaised.Should().BeFalse();
        }

        [TestMethod]
        public void UpdateMarkers_NoIssues_EventRaised()
        {
            var testSubject = new IssueTagger(ValidMarkerList, null);

            var eventRaised = false;
            testSubject.TagsChanged += (sender, e) => eventRaised = true;

            testSubject.UpdateMarkers(null, ValidSnapshotSpan);

            eventRaised.Should().BeTrue();
        }

        [TestMethod]
        public void UpdateMarkers_ValidIssuesAndSpan_EventRaised()
        {
            var testSubject = new IssueTagger(null, null);

            var eventRaised = false;
            testSubject.TagsChanged += (sender, e) => eventRaised = true;

            testSubject.UpdateMarkers(ValidMarkerList, ValidSnapshotSpan);

            eventRaised.Should().BeTrue();
        }

        [TestMethod]
        public void GetTags_NullIssues_ReturnEmptyList()
        {
            var testSubject = new IssueTagger(null, null);
            var normalizedSpans = new NormalizedSnapshotSpanCollection();

            var result = testSubject.GetTags(normalizedSpans);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_HasIntersectingIssues_ReturnsIntersectingIssues()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.Setup(x => x.Length).Returns(100);

            var span1 = new Span(0, 0);
            var span2 = new Span(1, 1);

            var snapshotSpan1 = new SnapshotSpan(mockTextSnapshot.Object, span1);
            var snapshotSpan2 = new SnapshotSpan(mockTextSnapshot.Object, span2);

            var marker1 = new IssueMarker(Mock.Of<IAnalysisIssue>(), snapshotSpan1, null, null);
            var marker2 = new IssueMarker(Mock.Of<IAnalysisIssue>(), snapshotSpan2, null, null);

            var markerList = new[] { marker1, marker2 };
            var normalizedSpans = new NormalizedSnapshotSpanCollection(snapshotSpan2);

            var testSubject = new IssueTagger(markerList, null);

            var result = testSubject.GetTags(normalizedSpans);

            result.Should().HaveCount(1);            
            result.First().Span.Should().Be(snapshotSpan2);
        }

        [TestMethod]
        public void Dispose_CallsDelegateOnlyOnce()
        {
            var delegateCallCount = 0;
            IssueTagger taggerArgument = null;
            IssueTagger.OnTaggerDisposed onTaggerDisposed = x =>
            {
                delegateCallCount++;
                taggerArgument = x;
            };

            var testSubject = new IssueTagger(Enumerable.Empty<IssueMarker>(), onTaggerDisposed);

            // 1. Delegate not called before disposal
            taggerArgument.Should().BeNull();

            // 2. Called on disposal
            testSubject.Dispose();
            taggerArgument.Should().BeSameAs(testSubject);
            delegateCallCount.Should().Be(1);

            // 3. Not call on subsequent disposal
            testSubject.Dispose();
            delegateCallCount.Should().Be(1);
        }

        private static IssueMarker CreateValidMarker() =>
            new IssueMarker(Mock.Of<IAnalysisIssue>(), ValidSnapshotSpan, null, null);
    }
}
