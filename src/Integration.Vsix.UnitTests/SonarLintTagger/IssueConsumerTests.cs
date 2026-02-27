/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class IssueConsumerTests
    {
        private IssueConsumerFactory.IIssueHandler issueHandler;
        private static readonly IAnalysisIssue ValidIssue = CreateIssue(startLine: 1, endLine: 1);
        private static readonly ITextSnapshot ValidTextSnapshot = CreateSnapshot(lineCount: 10);
        private static readonly IAnalysisIssueVisualizationConverter ValidConverter = Mock.Of<IAnalysisIssueVisualizationConverter>();

        private const string ValidFilePath = "c:\\myfile.txt";

        [TestInitialize]
        public void TestInitialize()
        {
            issueHandler = Substitute.For<IssueConsumerFactory.IIssueHandler>();
        }

        [TestMethod]
        public void Ctor_InvalidArgs_Throws()
        {
            Action act = () => new IssueConsumer(null, ValidFilePath, issueHandler, ValidConverter, "project");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("analysisSnapshot");

            act = () => new IssueConsumer(ValidTextSnapshot, null, issueHandler, ValidConverter, "project");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("analysisFilePath");

            act = () => new IssueConsumer(ValidTextSnapshot, ValidFilePath, null, ValidConverter, "project");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issueHandler");

            act = () => new IssueConsumer(ValidTextSnapshot, ValidFilePath, issueHandler, null, "project");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issueToIssueVisualizationConverter");
        }

        [TestMethod]
        public void SetIssues_WrongFile_CallbackIsNotCalled()
        {
            var issues = new IAnalysisIssue[] { ValidIssue };

            var testSubject = new IssueConsumer(ValidTextSnapshot, "c:\\file1.txt", issueHandler, ValidConverter, "project");

            testSubject.SetIssues("wrong file", issues);

            issueHandler.DidNotReceiveWithAnyArgs().HandleNewIssues(default);
            issueHandler.DidNotReceiveWithAnyArgs().HandleNewHotspots(default);
        }

        [TestMethod]
        public void SetHotspots_WrongFile_CallbackIsNotCalled()
        {
            var issues = new IAnalysisIssue[] { ValidIssue };

            var testSubject = new IssueConsumer(ValidTextSnapshot, "c:\\file1.txt", issueHandler, ValidConverter, "project");
            testSubject.SetHotspots("wrong file", issues);

            issueHandler.DidNotReceiveWithAnyArgs().HandleNewIssues(default);
            issueHandler.DidNotReceiveWithAnyArgs().HandleNewHotspots(default);
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
        public void SetIssues_IssuesNotInSnapshotAreIgnored_CallbackIsCalledWithExpectedIssues(int issueStartLine, int issueEndLine, bool isMappableToSnapshot)
        {
            // Issues are 1-based.
            // Snapshots are 0-based so last line = index 9
            const int LinesInSnapshot = 10;
            var snapshot = CreateSnapshot(LinesInSnapshot);
            var issues = new[] { CreateIssue(issueStartLine, issueEndLine) };
            var converter = CreatePassthroughConverter();

            var testSubject = new IssueConsumer(snapshot, ValidFilePath, issueHandler, converter, "project");

            testSubject.SetIssues(ValidFilePath, issues);

            ValidateReceivedIssues(isMappableToSnapshot ? issues : []);
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
        public void SetHotspots_IssuesNotInSnapshotAreIgnored_CallbackIsCalledWithExpectedIssues(int issueStartLine, int issueEndLine, bool isMappableToSnapshot)
        {
            // Issues are 1-based.
            // Snapshots are 0-based so last line = index 9
            const int LinesInSnapshot = 10;
            var snapshot = CreateSnapshot(LinesInSnapshot);
            var hotspots = new[] { CreateIssue(issueStartLine, issueEndLine) };
            var converter = CreatePassthroughConverter();

            var testSubject = new IssueConsumer(snapshot, ValidFilePath, issueHandler, converter, "project");

            testSubject.SetHotspots(ValidFilePath, hotspots);

            ValidateReceivedHotspots(isMappableToSnapshot ? hotspots : []);
        }

        [TestMethod]
        public void SetIssues_HasFileLevelIssues_NotIgnored()
        {
            var snapshot = CreateSnapshot(10);
            var issues = new[] { CreateFileLevelIssue() };
            var converter = CreatePassthroughConverter();

            var testSubject = new IssueConsumer(snapshot, ValidFilePath, issueHandler, converter, "project");

            testSubject.SetIssues(ValidFilePath, issues);

            ValidateReceivedIssues(issues);
        }

        [TestMethod]
        public void SetHotspots_HasFileLevelIssues_NotIgnored()
        {
            var snapshot = CreateSnapshot(10);
            var hotspots = new[] { CreateFileLevelIssue() };
            var converter = CreatePassthroughConverter();

            var testSubject = new IssueConsumer(snapshot, ValidFilePath, issueHandler, converter, "project");

            testSubject.SetHotspots(ValidFilePath, hotspots);

            ValidateReceivedHotspots(hotspots);
        }

        [TestMethod]
        public void SetIssues_MultipleCallsToAccept_IssuesAreReplaced()
        {
            var firstSetOfIssues = new[] { CreateIssue(1, 1), CreateIssue(2, 2) };

            var secondSetOfIssues = new[] { CreateIssue(3, 3), CreateIssue(4, 4) };

            var snapshot = CreateSnapshot(lineCount: 10);
            var converter = CreatePassthroughConverter();

            var testSubject = new IssueConsumer(snapshot, ValidFilePath, issueHandler, converter, "project");

            // 1. First call
            testSubject.SetIssues(ValidFilePath, firstSetOfIssues);

            ValidateReceivedIssues(firstSetOfIssues);
            issueHandler.ClearReceivedCalls();

            // 2. Second call
            testSubject.SetIssues(ValidFilePath, secondSetOfIssues);

            ValidateReceivedIssues(secondSetOfIssues);
        }

        [TestMethod]
        public void SetHotspots_MultipleCallsToAccept_IssuesAreReplaced()
        {
            var firstSetOfHotspots = new[] { CreateIssue(1, 1), CreateIssue(2, 2) };

            var secondSetOfHotspots = new[] { CreateIssue(3, 3), CreateIssue(4, 4) };

            var snapshot = CreateSnapshot(lineCount: 10);
            var converter = CreatePassthroughConverter();

            var testSubject = new IssueConsumer(snapshot, ValidFilePath, issueHandler, converter, "project");

            // 1. First call
            testSubject.SetHotspots(ValidFilePath, firstSetOfHotspots);

            ValidateReceivedHotspots(firstSetOfHotspots);
            issueHandler.ClearReceivedCalls();

            // 2. Second call
            testSubject.SetHotspots(ValidFilePath, secondSetOfHotspots);

            ValidateReceivedHotspots(secondSetOfHotspots);
        }

        private void ValidateReceivedIssues(IAnalysisIssue[] issues)
        {
            issueHandler.ReceivedWithAnyArgs(1).HandleNewIssues(default);
            issueHandler.DidNotReceiveWithAnyArgs().HandleNewHotspots(default);
            var analysisIssues = issueHandler.ReceivedCalls().Single().GetArguments()[0] as IEnumerable<IAnalysisIssueVisualization>;
            analysisIssues.Select(x => x.Issue).Should().BeEquivalentTo(issues);
        }

        private void ValidateReceivedHotspots(IAnalysisIssue[] hotspots)
        {
            issueHandler.ReceivedWithAnyArgs(1).HandleNewHotspots(default);
            issueHandler.DidNotReceiveWithAnyArgs().HandleNewIssues(default);
            var analysisIssues = issueHandler.ReceivedCalls().Single().GetArguments()[0] as IEnumerable<IAnalysisIssueVisualization>;
            analysisIssues.Select(x => x.Issue).Should().BeEquivalentTo(hotspots);
        }

        private static ITextSnapshot CreateSnapshot(int lineCount)
        {
            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.LineCount).Returns(lineCount);
            return mockSnapshot.Object;
        }

        private static IAnalysisIssue CreateIssue(int startLine, int endLine) =>
            new DummyAnalysisIssue { PrimaryLocation = new DummyAnalysisIssueLocation { TextRange = new DummyTextRange { StartLine = startLine, EndLine = endLine, }, Message = "any message" } };

        private static IAnalysisIssue CreateFileLevelIssue()
        {
            return new DummyAnalysisIssue { PrimaryLocation = new DummyAnalysisIssueLocation { TextRange = null } };
        }

        private static IAnalysisIssueVisualizationConverter CreatePassthroughConverter()
        {
            // Set up an issue converter that just wraps and returns the supplied issues as IssueVisualizations
            var mockIssueConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            mockIssueConverter
                .Setup(x => x.Convert(It.IsAny<IAnalysisIssue>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>()))
                .Returns<IAnalysisIssue, ITextSnapshot, string>((issue, snapshot, projectName) => CreateIssueViz(issue, new SnapshotSpan()));

            return mockIssueConverter.Object;

            IAnalysisIssueVisualization CreateIssueViz(IAnalysisIssue issue, SnapshotSpan snapshotSpan)
            {
                var issueVizMock = new Mock<IAnalysisIssueVisualization>();
                issueVizMock.Setup(x => x.Issue).Returns(issue);
                issueVizMock.Setup(x => x.Span).Returns(snapshotSpan);

                return issueVizMock.Object;
            }
        }
    }
}
