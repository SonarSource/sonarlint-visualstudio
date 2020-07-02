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

using System;
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
    public class AccumulatingIssueConsumerTests
    {
        private static readonly IAnalysisIssue ValidIssue = CreateIssue(startLine: 1, endline: 1);
        private static readonly ITextSnapshot ValidTextSnapshot = CreateSnapshot(lineCount: 10);
        private const string ValidFilePath = "c:\\myfile.txt";

        [TestMethod]
        public void Ctor_InvalidArgs_Throws()
        {
            AccumulatingIssueConsumer.OnIssuesChanged validCallback = _ => { };

            Action act = () => new AccumulatingIssueConsumer(null, ValidFilePath, validCallback);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("analysisSnapshot");

            act = () => new AccumulatingIssueConsumer(ValidTextSnapshot, null, validCallback);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("analysisFilePath");

            act = () => new AccumulatingIssueConsumer(ValidTextSnapshot, ValidFilePath, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("onIssuesChangedCallback");
        }

        [TestMethod]
        public void Accept_WrongFile_CallbackIsNotCalled()
        {
            var callbackSpy = new OnIssuesChangedCallbackSpy();
            var issues = new IAnalysisIssue[] { ValidIssue };

            var testSubject = new AccumulatingIssueConsumer(ValidTextSnapshot, "c:\\file1.txt", callbackSpy.Callback);

            using (new AssertIgnoreScope())
            {
                testSubject.Accept("wrong file", issues);
            }

            callbackSpy.CallCount.Should().Be(0);
        }

        [TestMethod]
        [DataRow(-1, 1, false)] // start line < 1
        [DataRow(0, 0, false)] // file-level issue, can't be mapped to snapshot
        [DataRow(0, 1, false)] // illegal i.e. shouldn't happen, but should be ignored if it does
        [DataRow(1, 1, true)] // starts in first line of snapshot
        [DataRow(9, 10, true)] // in snapshot
        [DataRow(10, 10, true)] // end is in last line of snapshot
        [DataRow(10, 11, false)] // end is outside snapshot
        [DataRow(11, 11, false)] // end is outside snapshot
        public void Accept_IssuesNotInSnapshotAreIgnored_CallbackIsCalledWithExpectedIssues(int issueStartLine, int issueEndLine, bool isMappableToSnapshot)
        {
            // Issues are 1-based.
            // Snapshots are 0-based so last line = index 9
            const int LinesInSnapshot = 10;
            var snapshot = CreateSnapshot(LinesInSnapshot);
            var issues = new[] { CreateIssue(issueStartLine, issueEndLine) };

            var callbackSpy = new OnIssuesChangedCallbackSpy();
            var converter = CreatePassthroughConverter();

            var testSubject = new AccumulatingIssueConsumer(snapshot, ValidFilePath, callbackSpy.Callback, converter);

            using (new AssertIgnoreScope())
            {
                testSubject.Accept(ValidFilePath, issues);
            }

            callbackSpy.CallCount.Should().Be(1);
            if (isMappableToSnapshot)
            {
                callbackSpy.LastSuppliedIssues.Should().BeEquivalentTo(issues);
            }
            else
            {
                callbackSpy.LastSuppliedMarkers.Should().BeEmpty();
            }
        }

        [TestMethod]
        public void Accept_MultipleCallsToAccept_IssuesAreAccumulated()
        {
            var callbackSpy = new OnIssuesChangedCallbackSpy();
            var firstSetOfIssues = new[]
            {
                CreateIssue(1, 1), CreateIssue(2, 2)
            };

            var secondSetOfIssues = new[]
            {
                CreateIssue(3,3), CreateIssue(4,4)
            };

            var snapshot = CreateSnapshot(lineCount: 10);
            var converter = CreatePassthroughConverter();

            var testSubject = new AccumulatingIssueConsumer(snapshot, ValidFilePath, callbackSpy.Callback, converter);

            // 1. First call
            testSubject.Accept(ValidFilePath, firstSetOfIssues);

            callbackSpy.CallCount.Should().Be(1);
            callbackSpy.LastSuppliedIssues.Should().BeEquivalentTo(firstSetOfIssues);

            // 2. Second call
            testSubject.Accept(ValidFilePath, secondSetOfIssues);

            callbackSpy.CallCount.Should().Be(2);
            callbackSpy.LastSuppliedIssues.Should().BeEquivalentTo(firstSetOfIssues.Union(secondSetOfIssues));
        }

        private class OnIssuesChangedCallbackSpy
        {
            public int CallCount { get; private set; }
            public IList<IssueMarker> LastSuppliedMarkers { get; private set; }
            public IList<IAnalysisIssue> LastSuppliedIssues
            {
                get
                {
                    return LastSuppliedMarkers?.Select(x => x.Issue).ToList();
                }
            }

            public void Callback(IEnumerable<IssueMarker> issueMarkers)
            {
                CallCount++;
                LastSuppliedMarkers = issueMarkers?.ToList();
            }
        }

        private static ITextSnapshot CreateSnapshot(int lineCount)
        {
            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.LineCount).Returns(lineCount);
            return mockSnapshot.Object;
        }

        private static IAnalysisIssue CreateIssue(int startLine, int endline) =>
            new DummyAnalysisIssue
            {
                StartLine = startLine,
                EndLine = endline,
                Message = "any message"
            };

        private static IIssueToIssueMarkerConverter CreatePassthroughConverter()
        {
            // Set up an issue converter that just wraps and returns the supplied issues as IssueMarkers
            var mockIssueConverter = new Mock<IIssueToIssueMarkerConverter>();
            mockIssueConverter.Setup(x => x.Convert(It.IsAny<IAnalysisIssue>(), It.IsAny<ITextSnapshot>()))
                .Returns<IAnalysisIssue, ITextSnapshot>((issue, snapshot) => new IssueMarker(issue, new SnapshotSpan(), null, null));
            return mockIssueConverter.Object;
        }
    }
}
