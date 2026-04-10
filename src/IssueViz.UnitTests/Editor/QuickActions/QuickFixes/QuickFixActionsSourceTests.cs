/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes;

[TestClass]
public class QuickFixActionsSourceTests
{
    private SnapshotSpan mockSpan;
    private ITextBuffer textBuffer;
    private ITextView textView;

    [TestInitialize]
    public void TestInitialize()
    {
        mockSpan = new SnapshotSpan();
        textView = CreateWpfTextView();
        textBuffer = CreateBuffer();
    }

    [TestMethod]
    public void IsSubclassOfIssueActionsSourceBase()
    {
        typeof(QuickFixActionsSource).Should().BeAssignableTo<IssueActionsSourceBase>();
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NoIssuesWithQuickFixes_False()
    {
        var issues = new[] { CreateIssueViz() };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().Be(false);
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NoIssuesWithApplicableQuickFixes_False()
    {
        var issues = new[] { CreateIssueViz(CreateQuickFixViz(canBeApplied: false)) };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().Be(false);
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_HasIssuesWithApplicableQuickFixes_True()
    {
        var issues = new[] { CreateIssueViz(CreateQuickFixViz(canBeApplied: true)) };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().Be(true);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetSuggestedActions_NoIssuesWithQuickFixes_NoActions(bool hasIssues)
    {
        var issues = hasIssues
            ? new[] { CreateIssueViz() }
            : Array.Empty<IAnalysisIssueVisualization>();

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

        var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        hasSuggestedActionsSet.Should().BeEmpty();
    }

    [TestMethod]
    public void GetSuggestedActions_NoIssuesWithApplicableQuickFixes_NoActions()
    {
        var issues = new[] { CreateIssueViz(CreateQuickFixViz(canBeApplied: false)) };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

        var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        hasSuggestedActionsSet.Should().BeEmpty();
    }

    [TestMethod]
    public void GetSuggestedActions_HasIssuesWithQuickFixes_OneActionForEveryApplicableFix()
    {
        var issues = new[]
        {
            CreateIssueViz(
                CreateQuickFixViz(canBeApplied: false, message: "fix1"),
                CreateQuickFixViz(canBeApplied: true, message: "fix2")),
            CreateIssueViz(
                CreateQuickFixViz(canBeApplied: true, message: "fix3"),
                CreateQuickFixViz(canBeApplied: false, message: "fix4")),
            CreateIssueViz(),
            CreateIssueViz(CreateQuickFixViz(canBeApplied: false, message: "fix5")),
            CreateIssueViz(CreateQuickFixViz(canBeApplied: true, message: "fix6"))
        };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

        var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);
        hasSuggestedActionsSet.Count().Should().Be(1);

        var quickFixSuggestedActions = hasSuggestedActionsSet.Single().Actions.OfType<QuickFixSuggestedAction>().ToList();
        quickFixSuggestedActions.Count.Should().Be(3);
        quickFixSuggestedActions.Select(x => x.DisplayText).Should().BeEquivalentTo(Resources.ProductNameCommandPrefix + "fix2", Resources.ProductNameCommandPrefix + "fix3",
            Resources.ProductNameCommandPrefix + "fix6");
    }

    private QuickFixActionsSource CreateTestSubject(
        ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator = null,
        ILightBulbBroker lightBulbBroker = null,
        IThreadHandling threadHandling = null)
    {
        issueLocationsTagAggregator ??= Mock.Of<ITagAggregator<IIssueLocationTag>>();
        lightBulbBroker ??= Mock.Of<ILightBulbBroker>();
        var loggerMock = new Mock<ILogger>();
        loggerMock.Setup(x => x.ForVerboseContext(It.IsAny<string[]>())).Returns(loggerMock.Object);

        var bufferTagAggregatorFactoryService = new Mock<IBufferTagAggregatorFactoryService>();

        bufferTagAggregatorFactoryService
            .Setup(x => x.CreateTagAggregator<IIssueLocationTag>(textBuffer))
            .Returns(issueLocationsTagAggregator);

        threadHandling ??= new NoOpThreadHandler();

        return new QuickFixActionsSource(lightBulbBroker,
            bufferTagAggregatorFactoryService.Object,
            textView,
            textBuffer,
            Mock.Of<IQuickFixesTelemetryManager>(),
            Substitute.For<IMessageBox>(),
            loggerMock.Object,
            threadHandling);
    }

    private IAnalysisIssueVisualization CreateIssueViz(params IQuickFixApplication[] fixes)
    {
        var issueViz = new Mock<IAnalysisIssueVisualization>();
        issueViz.Setup(x => x.QuickFixes).Returns(fixes);

        return issueViz.Object;
    }

    private Mock<ITagAggregator<IIssueLocationTag>> CreateTagAggregatorForIssues(IAnalysisIssueVisualization[] issues)
    {
        var issueTags = issues.Select(x => CreateMappingTagSpan(textBuffer.CurrentSnapshot, CreateIssueLocationTag(x), mockSpan)).ToArray();

        var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

        issueLocationsTagAggregator
            .Setup(x => x.GetTags(mockSpan))
            .Returns(issueTags);

        return issueLocationsTagAggregator;
    }

    private IQuickFixApplication CreateQuickFixViz(bool canBeApplied, string message = null)
    {
        var quickFixApplication = Substitute.For<IQuickFixApplication>();
        quickFixApplication.Message.Returns(message);
        quickFixApplication.CanBeApplied(textBuffer.CurrentSnapshot).Returns(canBeApplied);

        return quickFixApplication;
    }
}
