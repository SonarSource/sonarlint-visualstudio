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

        [TestInitialize]
        public void TestInitialize()
        {
            quickFixApplication = Substitute.For<IQuickFixApplication>();
            snapshot = CreateTextSnapshot();
            textBuffer = CreateTextBuffer(snapshot);
            issueViz = Substitute.For<IAnalysisIssueVisualization>();
            issueViz.RuleId.Returns(RuleId);
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
        public void DisplayName_ReturnsFixMessage()
        {
            const string message = "some fix";
            quickFixApplication.Message.Returns(message);

            testSubject.DisplayText.Should().Be(Resources.ProductNameCommandPrefix + message);
        }

        [TestMethod]
        public void Invoke_AppliesFixOnUiThreadWithTelemetry()
        {
            ConfigureQuickFixApplication();

            testSubject.Invoke(CancellationToken.None);

            Received.InOrder(() =>
            {
                quickFixApplication.CanBeApplied(snapshot);
                threadHandling.Run(Arg.Any<Func<Task<int>>>());
                threadHandling.SwitchToMainThreadAsync();
                quickFixApplication.ApplyAsync(snapshot, Arg.Any<CancellationToken>());
                telemetryManager.QuickFixApplied(RuleId);
            });
            issueViz.Received().Span = Arg.Is<SnapshotSpan>(x => x.Length == 0); // property assignments are not checked in Received.InOrder
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
            ConfigureNonApplicableQuickFixApplication();

            testSubject.Invoke(CancellationToken.None);

            quickFixApplication.Received(1).CanBeApplied(snapshot);
            quickFixApplication.DidNotReceiveWithAnyArgs().ApplyAsync(default, default);
            issueViz.DidNotReceiveWithAnyArgs().Span = Arg.Any<SnapshotSpan>();
            telemetryManager.DidNotReceiveWithAnyArgs().QuickFixApplied(Arg.Any<string>());
        }

        private static ITextSnapshot CreateTextSnapshot()
        {
            var snapshot = Substitute.For<ITextSnapshot>();
            snapshot.Length.Returns(int.MaxValue);
            return snapshot;
        }

        private void ConfigureQuickFixApplication() => ConfigureQuickFixApplication(true);

        private void ConfigureNonApplicableQuickFixApplication() => ConfigureQuickFixApplication(false);

        private void ConfigureQuickFixApplication(bool canBeApplied) =>
            quickFixApplication.CanBeApplied(snapshot)
                .Returns(canBeApplied);

        private static ITextBuffer CreateTextBuffer(ITextSnapshot snapShot)
        {
            var textBuffer = Substitute.For<ITextBuffer>();
            textBuffer.CurrentSnapshot.Returns(snapShot);
            return textBuffer;
        }
    }
}
