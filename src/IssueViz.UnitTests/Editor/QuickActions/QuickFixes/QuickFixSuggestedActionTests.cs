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
using SonarLint.VisualStudio.Core;
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
        private IQuickFixApplicationLogic quickFixApplicationLogic;
        private NoOpThreadHandler threadHandling;
        private ITextSnapshot snapshot;
        private QuickFixSuggestedAction testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            quickFixApplication = Substitute.For<IQuickFixApplication>();
            snapshot = CreateTextSnapshot();
            textBuffer = CreateTextBuffer(snapshot);
            issueViz = Substitute.For<IAnalysisIssueVisualization>();
            quickFixApplicationLogic = Substitute.For<IQuickFixApplicationLogic>();
            threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

            testSubject = new QuickFixSuggestedAction(
                quickFixApplication,
                textBuffer,
                issueViz,
                quickFixApplicationLogic,
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
        public void Invoke_DelegatesToQuickFixApplicationLogic()
        {
            quickFixApplicationLogic.ApplyAsync(quickFixApplication, snapshot, issueViz, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            testSubject.Invoke(CancellationToken.None);

            Received.InOrder(() =>
            {
                threadHandling.Run(Arg.Any<Func<Task<int>>>());
                threadHandling.SwitchToMainThreadAsync();
                quickFixApplicationLogic.ApplyAsync(quickFixApplication, snapshot, issueViz, Arg.Any<CancellationToken>());
            });
        }

        [TestMethod]
        public void Invoke_CancellationTokenIsCancelled_NoChanges()
        {
            testSubject.Invoke(new CancellationToken(canceled: true));

            quickFixApplicationLogic.DidNotReceiveWithAnyArgs()
                .ApplyAsync(default, default, default, default);
        }

        private static ITextSnapshot CreateTextSnapshot()
        {
            var snapshot = Substitute.For<ITextSnapshot>();
            snapshot.Length.Returns(int.MaxValue);
            return snapshot;
        }

        private static ITextBuffer CreateTextBuffer(ITextSnapshot snapShot)
        {
            var textBuffer = Substitute.For<ITextBuffer>();
            textBuffer.CurrentSnapshot.Returns(snapShot);
            return textBuffer;
        }
    }
}
