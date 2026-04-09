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

using System.Windows;
using Microsoft.VisualStudio.Text;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes;

[TestClass]
public class QuickFixApplicationLogicTests
{
    private IQuickFixesTelemetryManager telemetryManager = null!;
    private IMessageBox messageBox = null!;
    private TestLogger logger = null!;
    private IQuickFixApplication quickFixApplication = null!;
    private ITextSnapshot snapshot = null!;
    private IAnalysisIssueVisualization issueViz = null!;
    private QuickFixApplicationLogic testSubject = null!;

    private const string RuleId = "test-rule-id";
    private SnapshotSpan originalSpan;

    [TestInitialize]
    public void TestInitialize()
    {
        telemetryManager = Substitute.For<IQuickFixesTelemetryManager>();
        messageBox = Substitute.For<IMessageBox>();
        logger = Substitute.ForPartsOf<TestLogger>();

        quickFixApplication = Substitute.For<IQuickFixApplication>();
        snapshot = CreateTextSnapshot();
        issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.Issue.RuleKey.Returns(RuleId);

        originalSpan = new SnapshotSpan(snapshot, new Span(0, 10));
        issueViz.Span.Returns(originalSpan);

        testSubject = new QuickFixApplicationLogic(telemetryManager, messageBox, logger);
    }

    [TestMethod]
    public void CheckIsSingletonMefComponent() =>
        MefTestHelpers.CheckIsSingletonMefComponent<QuickFixApplicationLogic>();

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<QuickFixApplicationLogic, IQuickFixApplicationLogic>(
            MefTestHelpers.CreateExport<IQuickFixesTelemetryManager>(),
            MefTestHelpers.CreateExport<IMessageBox>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void Ctor_SetsLogContext() => logger.Received(1).ForContext(Resources.QuickFixSuggestedAction_LogContext);

    [TestMethod]
    public void CanBeApplied_DelegatesToQuickFixApplication()
    {
        quickFixApplication.CanBeApplied(snapshot).Returns(true);

        testSubject.CanBeApplied(quickFixApplication, snapshot).Should().BeTrue();

        quickFixApplication.Received(1).CanBeApplied(snapshot);
    }

    [TestMethod]
    public void CanBeApplied_ReturnsFalse_WhenQuickFixCannotBeApplied()
    {
        quickFixApplication.CanBeApplied(snapshot).Returns(false);

        testSubject.CanBeApplied(quickFixApplication, snapshot).Should().BeFalse();
    }

    [TestMethod]
    public async Task ApplyAsync_Success_InvalidatesSpanAndSendsTelemetry()
    {
        quickFixApplication.CanBeApplied(snapshot).Returns(true);
        quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>()).Returns(true);

        var result = await testSubject.ApplyAsync(quickFixApplication, snapshot, issueViz, CancellationToken.None);

        result.Should().BeTrue();
        issueViz.Received(1).Span = Arg.Is<SnapshotSpan>(s => s.IsEmpty);
        issueViz.DidNotReceive().Span = originalSpan;
        telemetryManager.Received(1).QuickFixApplied(RuleId);
    }

    [TestMethod]
    public async Task ApplyAsync_ApplicationReturnsFalse_SpanIsRestored_TelemetryNotSent()
    {
        quickFixApplication.CanBeApplied(snapshot).Returns(true);
        quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>()).Returns(false);

        var result = await testSubject.ApplyAsync(quickFixApplication, snapshot, issueViz, CancellationToken.None);

        result.Should().BeFalse();
        issueViz.Received().Span = originalSpan;
        telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
        VerifyUserNotified();
    }

    [TestMethod]
    public async Task ApplyAsync_ApplicationThrowsException_SpanIsRestored_TelemetryNotSent()
    {
        quickFixApplication.CanBeApplied(snapshot).Returns(true);
        var exception = new Exception("test");
        quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        var act = () => testSubject.ApplyAsync(quickFixApplication, snapshot, issueViz, CancellationToken.None);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().Be(exception);
        issueViz.Received().Span = originalSpan;
        telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
    }

    [TestMethod]
    public async Task ApplyAsync_CancellationRequested_ReturnsFalse_NoChanges()
    {
        var result = await testSubject.ApplyAsync(quickFixApplication, snapshot, issueViz, new CancellationToken(canceled: true));

        result.Should().BeFalse();
        quickFixApplication.DidNotReceiveWithAnyArgs().CanBeApplied(default);
        quickFixApplication.DidNotReceiveWithAnyArgs().ApplyAsync(default, default);
        issueViz.DidNotReceiveWithAnyArgs().Span = Arg.Any<SnapshotSpan>();
        telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
    }

    [TestMethod]
    public async Task ApplyAsync_QuickFixNotApplicable_ReturnsFalse_NoChanges()
    {
        quickFixApplication.CanBeApplied(snapshot).Returns(false);

        var result = await testSubject.ApplyAsync(quickFixApplication, snapshot, issueViz, CancellationToken.None);

        result.Should().BeFalse();
        quickFixApplication.Received(1).CanBeApplied(snapshot);
        quickFixApplication.DidNotReceiveWithAnyArgs().ApplyAsync(default, default);
        issueViz.DidNotReceiveWithAnyArgs().Span = Arg.Any<SnapshotSpan>();
        telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
    }

    private void VerifyUserNotified()
    {
        var expectedMessage = string.Format(Resources.QuickFixSuggestedAction_CouldNotApply, RuleId);
        messageBox.Received(1).Show(
            expectedMessage,
            Resources.QuickFixSuggestedAction_CouldNotApplyMessageBoxCaption,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static ITextSnapshot CreateTextSnapshot()
    {
        var snapshot = Substitute.For<ITextSnapshot>();
        snapshot.Length.Returns(int.MaxValue);
        return snapshot;
    }
}
