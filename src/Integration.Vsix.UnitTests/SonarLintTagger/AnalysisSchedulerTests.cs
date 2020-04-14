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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class AnalysisSchedulerTests
    {
        private AnalysisScheduler testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            var mockLogger = new Mock<ILogger>();
            testSubject = new AnalysisScheduler(mockLogger.Object);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Sequential_EachAnalysisIsCalled()
        {
            var firstAnalysis = SetupAnalyzeAction(out var getFirstToken);
            var secondAnalysis = SetupAnalyzeAction(out var getSecondToken);
            var thirdAnalysis = SetupAnalyzeAction(out var getThirdToken);

            testSubject.Schedule("test path", firstAnalysis.Object);
            testSubject.Schedule("test path", secondAnalysis.Object);
            testSubject.Schedule("test path", thirdAnalysis.Object);

            var firstToken = getFirstToken();
            firstAnalysis.Verify(x => x(firstToken), Times.Once);

            var secondToken = getSecondToken();
            secondAnalysis.Verify(x => x(secondToken), Times.Once);

            var thirdToken = getThirdToken();
            thirdAnalysis.Verify(x => x(thirdToken), Times.Once);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Sequential_EachCallHasNewToken()
        {
            var firstAnalysis = SetupAnalyzeAction(out var getFirstToken);
            var secondAnalysis = SetupAnalyzeAction(out var getSecondToken);
            var thirdAnalysis = SetupAnalyzeAction(out var getThirdToken);

            testSubject.Schedule("test path", firstAnalysis.Object);
            testSubject.Schedule("test path", secondAnalysis.Object);
            testSubject.Schedule("test path", thirdAnalysis.Object);

            var firstToken = getFirstToken();
            var secondToken = getSecondToken();
            var thirdToken = getThirdToken();

            firstToken.Should().NotBeEquivalentTo(secondToken);
            secondToken.Should().NotBeEquivalentTo(thirdToken);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Sequential_EachAnalysisIsFinished()
        {
            var firstAnalysis = SetupAnalyzeAction(out var getFirstToken);
            var secondAnalysis = SetupAnalyzeAction(out var getSecondToken);
            var thirdAnalysis = SetupAnalyzeAction(out var getThirdToken);

            testSubject.Schedule("test path", firstAnalysis.Object);
            testSubject.Schedule("test path", secondAnalysis.Object);
            testSubject.Schedule("test path", thirdAnalysis.Object);

            var tokens = new List<CancellationToken>
            {
                getFirstToken(),
                getSecondToken(),
                getThirdToken()
            };

            tokens.All(x => !x.IsCancellationRequested).Should().BeTrue("No token should be cancelled");
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Parallel_EachAnalysisIsCalled()
        {
            var firstAnalysis = SetupAnalyzeAction(out var getFirstToken, sleepInMiliseconds: 300);
            var secondAnalysis = SetupAnalyzeAction(out var getSecondToken, sleepInMiliseconds: 200);
            var thirdAnalysis = SetupAnalyzeAction(out var getThirdToken, sleepInMiliseconds: 100);

            var firstAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", firstAnalysis.Object));
            var secondAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", secondAnalysis.Object));
            var thirdAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", thirdAnalysis.Object));

            Task.WaitAll(firstAnalysisTask, secondAnalysisTask, thirdAnalysisTask);

            var firstToken = getFirstToken();
            firstAnalysis.Verify(x => x(firstToken), Times.Once);

            var secondToken = getSecondToken();
            secondAnalysis.Verify(x => x(secondToken), Times.Once);

            var thirdToken = getThirdToken();
            thirdAnalysis.Verify(x => x(thirdToken), Times.Once);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Parallel_EachCallHasNewToken()
        {
            var firstAnalysis = SetupAnalyzeAction(out var getFirstToken, sleepInMiliseconds: 300);
            var secondAnalysis = SetupAnalyzeAction(out var getSecondToken, sleepInMiliseconds: 200);
            var thirdAnalysis = SetupAnalyzeAction(out var getThirdToken, sleepInMiliseconds: 100);

            var firstAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", firstAnalysis.Object));
            var secondAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", secondAnalysis.Object));
            var thirdAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", thirdAnalysis.Object));

            Task.WaitAll(firstAnalysisTask, secondAnalysisTask, thirdAnalysisTask);

            var firstToken = getFirstToken();
            var secondToken = getSecondToken();
            var thirdToken = getThirdToken();

            firstToken.Should().NotBeEquivalentTo(secondToken);
            secondToken.Should().NotBeEquivalentTo(thirdToken);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Parallel_PreviousAnalysisIsCancelledAndOnlyLatestIsFinished()
        {
            var firstAnalysis = SetupAnalyzeAction(out var getFirstToken, sleepInMiliseconds: 300);
            var secondAnalysis = SetupAnalyzeAction(out var getSecondToken, sleepInMiliseconds: 100);
            var thirdAnalysis = SetupAnalyzeAction(out var getThirdToken, sleepInMiliseconds: 200);

            var firstAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", firstAnalysis.Object));
            var secondAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", secondAnalysis.Object));
            var thirdAnalysisTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", thirdAnalysis.Object));

            Task.WaitAll(firstAnalysisTask, secondAnalysisTask, thirdAnalysisTask);

            var tokens = new List<CancellationToken>
            {
                getFirstToken(),
                getSecondToken(),
                getThirdToken()
            };

            tokens.Count(x => !x.IsCancellationRequested).Should().Be(1, "Only one token should not be cancelled");
        }

        private static Mock<Action<CancellationToken>> SetupAnalyzeAction(out Func<CancellationToken> getCreatedToken, int? sleepInMiliseconds = null)
        {
            CancellationToken cancellationToken;
            var analyzeAction = new Mock<Action<CancellationToken>>();
            analyzeAction
                .Setup(x => x(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken token) =>
                {
                    if (sleepInMiliseconds.HasValue)
                    {
                        Thread.Sleep(sleepInMiliseconds.Value);
                    }
                    cancellationToken = token;
                });

            getCreatedToken = () => cancellationToken;

            return analyzeAction;
        }
    }
}
