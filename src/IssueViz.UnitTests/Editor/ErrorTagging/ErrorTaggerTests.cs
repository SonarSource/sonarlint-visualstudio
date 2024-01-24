/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class ErrorTaggerTests
    {
        [TestMethod]
        public void GetTags_FilterIsApplied_ExpectedTagsCreated()
        {
            var snapshot = CreateSnapshot(length: 50);

            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var primary1 = CreateTagSpanWithPrimaryLocation(snapshot, new Span(1, 5), message: "error message 1", ruleKey: "cpp:S5350");
            var primary1_Suppressed = CreateTagSpanWithPrimaryLocation(snapshot, new Span(2, 6));
            var primary2 = CreateTagSpanWithPrimaryLocation(snapshot, new Span(10, 5), message: "error message 2", ruleKey: "cpp:emptyCompoundStatement");
            var primary2_Suppressed = CreateTagSpanWithPrimaryLocation(snapshot, new Span(8, 15));
            var secondary1 = CreateTagSpanWithSecondaryLocation(snapshot, new Span(20, 5));
            var secondary2 = CreateTagSpanWithSecondaryLocation(snapshot, new Span(30, 5));
            var aggregator = CreateAggregator(primary1, secondary1, primary1_Suppressed, primary2, secondary2, primary2_Suppressed);

            var tooltipProvider = new Mock<IErrorTagTooltipProvider>();
            var issue1 = (primary1.Tag.Location as IAnalysisIssueVisualization).Issue;
            var issue2 = (primary2.Tag.Location as IAnalysisIssueVisualization).Issue;

            tooltipProvider.Setup(x => x.Create(issue1)).Returns("some tooltip1");
            tooltipProvider.Setup(x => x.Create(issue2)).Returns("some tooltip2");

            var issueViz1 = (primary1_Suppressed.Tag.Location as IAnalysisIssueVisualization);
            issueViz1.IsSuppressed = true;
            var issueViz2 = (primary2_Suppressed.Tag.Location as IAnalysisIssueVisualization);
            issueViz2.IsSuppressed = true;

            var testSubject = new ErrorTagger(aggregator, snapshot.TextBuffer, tooltipProvider.Object);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual[0].Tag.ToolTipContent.Should().Be("some tooltip1");
            actual[0].Span.Span.Should().Be(primary1.Tag.Location.Span.Value.Span);

            actual[1].Tag.ToolTipContent.Should().Be("some tooltip2");
            actual[1].Span.Span.Should().Be(primary2.Tag.Location.Span.Value.Span);

            actual.Length.Should().Be(2);
        }

        [TestMethod]
        public void GetTags_EmptySpansShouldBeFiltered()
        {
            var snapshot = CreateSnapshot(length: 50);

            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);
            
            var validSpan = new Span(1, 5);
            var invalidSpan = new Span(10, 0);

            var primary1 = CreateTagSpanWithPrimaryLocation(snapshot, validSpan, message: "error message 1", ruleKey: "cpp:S5350");
            var primary2 = CreateTagSpanWithPrimaryLocation(snapshot, invalidSpan, message: "error message 2", ruleKey: "cpp:emptyCompoundStatement");
            var aggregator = CreateAggregator(primary1, primary2);

            var tooltipProvider = new Mock<IErrorTagTooltipProvider>();
            var issue1 = (primary1.Tag.Location as IAnalysisIssueVisualization).Issue;
            var issue2 = (primary2.Tag.Location as IAnalysisIssueVisualization).Issue;
            tooltipProvider.Setup(x => x.Create(issue1)).Returns("some tooltip1");
            tooltipProvider.Setup(x => x.Create(issue2)).Returns("some tooltip2");

            var testSubject = new ErrorTagger(aggregator, snapshot.TextBuffer, tooltipProvider.Object);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual.Length.Should().Be(1);

            actual[0].Tag.ToolTipContent.Should().Be("some tooltip1");
            actual[0].Span.Span.Should().Be(validSpan);
        }

        private static IMappingTagSpan<IIssueLocationTag> CreateTagSpanWithPrimaryLocation(ITextSnapshot snapshot, Span span, string message = "", string ruleKey = "")
        {
            var viz = CreateIssueViz(snapshot, span, message, ruleKey);
            var tag = CreateIssueLocationTag(viz);
            return CreateMappingTagSpan(snapshot, tag, span);
        }

        private static IMappingTagSpan<IIssueLocationTag> CreateTagSpanWithSecondaryLocation(ITextSnapshot snapshot, Span span, string errorMessage = "")
        {
            var viz = CreateLocationViz(snapshot, span, errorMessage);
            var tag = CreateIssueLocationTag(viz);
            return CreateMappingTagSpan(snapshot, tag, span);
        }
    }
}
