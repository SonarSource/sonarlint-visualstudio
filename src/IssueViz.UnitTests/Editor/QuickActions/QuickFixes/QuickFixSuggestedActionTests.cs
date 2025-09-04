/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes
{
    [TestClass]
    public class QuickFixSuggestedActionTests
    {
        private IQuickFixApplication quickFixApplication;
        private ITextBuffer textBuffer;
        private IAnalysisIssueVisualization issueViz;
        private IQuickFixesTelemetryManager telemetryManager;
        private TestLogger logger;
        private NoOpThreadHandler threadHandling;
        private ITextSnapshot snapshot;
        private QuickFixSuggestedAction testSubject;
        private const string RuleId = "test-rule-id";
        private SnapshotSpan originalSpan;

        [TestInitialize]
        public void TestInitialize()
        {
            quickFixApplication = Substitute.For<IQuickFixApplication>();
            snapshot = CreateTextSnapshot();
            textBuffer = CreateTextBuffer(snapshot);
            issueViz = Substitute.For<IAnalysisIssueVisualization>();
            issueViz.RuleId.Returns(RuleId);

            // Set up a non-empty span
            originalSpan = new SnapshotSpan(snapshot, new Span(0, 10));
            issueViz.Span.Returns(originalSpan);

            telemetryManager = Substitute.For<IQuickFixesTelemetryManager>();
            logger = Substitute.ForPartsOf<TestLogger>();
            threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

            testSubject = new QuickFixSuggestedAction(
                quickFixApplication,
                textBuffer,
                issueViz,
                telemetryManager,
                logger,
                threadHandling);
        }

        [TestMethod]
        public void Ctor_SetsLogContext() => logger.Received(1).ForContext(Resources.QuickFixSuggestedAction_LogContext);

        [TestMethod]
        public void DisplayName_ReturnsFixMessage()
        {
            const string message = "some fix";
            quickFixApplication.Message.Returns(message);

            testSubject.DisplayText.Should().Be(Resources.ProductNameCommandPrefix + message);
        }

        [TestMethod]
        public void Invoke_AppliesFixOnUiThreadWithTelemetry()
        {
            ConfigureQuickFixApplicationCanBeApplied(true, true);

            testSubject.Invoke(CancellationToken.None);

            Received.InOrder(() =>
            {
                quickFixApplication.CanBeApplied(snapshot);
                threadHandling.Run(Arg.Any<Func<Task<int>>>());
                threadHandling.SwitchToMainThreadAsync();
                quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>());
                telemetryManager.QuickFixApplied(RuleId);
            });
            issueViz.Received(1).Span = Arg.Is<SnapshotSpan>(s => s.IsEmpty);
            issueViz.DidNotReceive().Span = originalSpan;
        }

        [TestMethod]
        public void Invoke_QuickFixApplicationReturnsFalse_SpanIsRestored_TelemetryNotSent()
        {
            ConfigureQuickFixApplicationCanBeApplied(true, false);

            testSubject.Invoke(CancellationToken.None);

            VerifyDidNotApply();
        }

        [TestMethod]
        public void Invoke_QuickFixApplicationThrowsException_SpanIsRestored_TelemetryNotSent()
        {
            quickFixApplication.CanBeApplied(snapshot).Returns(true);
            var exception = new Exception("test");
            quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>()).ThrowsAsync(exception);

            var act = () => testSubject.Invoke(CancellationToken.None);

            act.Should().Throw<Exception>().Which.Should().Be(exception);
            VerifyDidNotApply();
        }

        [TestMethod]
        public void Invoke_CancellationTokenIsCancelled_NoChanges()
        {
            testSubject.Invoke(new CancellationToken(canceled: true));

            quickFixApplication.DidNotReceiveWithAnyArgs().ApplyAsync(default, default);
            issueViz.DidNotReceiveWithAnyArgs().Span = Arg.Any<SnapshotSpan>();
            telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
        }

        [TestMethod]
        public void Invoke_QuickFixIsNotApplicable_NoChanges()
        {
            ConfigureQuickFixApplicationCanBeApplied(false, false);

            testSubject.Invoke(CancellationToken.None);

            quickFixApplication.Received(1).CanBeApplied(snapshot);
            quickFixApplication.DidNotReceiveWithAnyArgs().ApplyAsync(default, default);
            issueViz.DidNotReceiveWithAnyArgs().Span = Arg.Any<SnapshotSpan>();
            telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
        }

        private void VerifyDidNotApply()
        {
            Received.InOrder(() =>
            {
                quickFixApplication.CanBeApplied(snapshot);
                threadHandling.Run(Arg.Any<Func<Task<int>>>());
                threadHandling.SwitchToMainThreadAsync();
                quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>());
            });
            issueViz.Received().Span = originalSpan;
            telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
            logger.AssertPartialOutputStringExists(string.Format(Resources.QuickFixSuggestedAction_CouldNotApply, RuleId));
        }

        private static ITextSnapshot CreateTextSnapshot()
        {
            var snapshot = Substitute.For<ITextSnapshot>();
            snapshot.Length.Returns(int.MaxValue);
            return snapshot;
        }

        private void ConfigureQuickFixApplicationCanBeApplied(bool canBeApplied, bool willBeApplied)
        {
            quickFixApplication.CanBeApplied(snapshot)
                .Returns(canBeApplied);

            quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>())
                .Returns(willBeApplied);
        }

        private static ITextBuffer CreateTextBuffer(ITextSnapshot snapShot)
        {
            var textBuffer = Substitute.For<ITextBuffer>();
            textBuffer.CurrentSnapshot.Returns(snapShot);
            return textBuffer;
        }
    }
}
