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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class SchedulerTests
    {
        private Scheduler testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new Scheduler();
        }

        [TestMethod]
        public void Schedule_JobReachedTimeout_JobCancelledDueToTimeOut()
        {
            var job = SetupJobAction(out var getToken);

            testSubject.Schedule("test path", job.Object, 1000);

            var token = getToken();
            token.IsCancellationRequested.Should().BeFalse();

            // wait for job to time out
            Thread.Sleep(3000);

            token.IsCancellationRequested.Should().BeTrue();
            token.IsTimedOut().Should().BeTrue();
        }

        [TestMethod]
        public void Schedule_JobDidNotReachTimeout_JobCancelledDueToSecondRetrigger()
        {
            var firstJob = SetupJobAction(out var getFirstToken);
            var secondJob = SetupJobAction(out _);

            testSubject.Schedule("test path", firstJob.Object, 3000);

            var firstToken = getFirstToken();
            firstToken.IsCancellationRequested.Should().BeFalse();

            testSubject.Schedule("test path", secondJob.Object, Timeout.Infinite);

            firstToken.IsCancellationRequested.Should().BeTrue();
            firstToken.IsTimedOut().Should().BeFalse();
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Sequential_EachJobIsCalled()
        {
            var firstJob = SetupJobAction(out var getFirstToken);
            var secondJob = SetupJobAction(out var getSecondToken);
            var thirdJob = SetupJobAction(out var getThirdToken);

            testSubject.Schedule("test path", firstJob.Object, Timeout.Infinite);
            testSubject.Schedule("test path", secondJob.Object, Timeout.Infinite);
            testSubject.Schedule("test path", thirdJob.Object, Timeout.Infinite);

            var firstToken = getFirstToken();
            firstJob.Verify(x => x(firstToken), Times.Once);

            var secondToken = getSecondToken();
            secondJob.Verify(x => x(secondToken), Times.Once);

            var thirdToken = getThirdToken();
            thirdJob.Verify(x => x(thirdToken), Times.Once);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Sequential_TimeOut_EachJobIsCancelled()
        {
            var firstJob = SetupJobAction(out var getFirstToken);
            var secondJob = SetupJobAction(out var getSecondToken);
            var thirdJob = SetupJobAction(out var getThirdToken);

            testSubject.Schedule("test path", firstJob.Object, 1);
            Thread.Sleep(100);

            var firstToken = getFirstToken();
            firstToken.IsCancellationRequested.Should().BeTrue();
            firstJob.Verify(x => x(firstToken), Times.Once);

            testSubject.Schedule("test path", secondJob.Object, 1);
            Thread.Sleep(100);

            var secondToken = getSecondToken();
            secondToken.IsCancellationRequested.Should().BeTrue();
            secondJob.Verify(x => x(secondToken), Times.Once);

            testSubject.Schedule("test path", thirdJob.Object, Timeout.Infinite);

            var thirdToken = getThirdToken();
            thirdToken.IsCancellationRequested.Should().BeFalse();
            thirdJob.Verify(x => x(thirdToken), Times.Once);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Sequential_EachCallHasNewToken()
        {
            var firstJob = SetupJobAction(out var getFirstToken);
            var secondJob = SetupJobAction(out var getSecondToken);
            var thirdJob = SetupJobAction(out var getThirdToken);

            testSubject.Schedule("test path", firstJob.Object, Timeout.Infinite);
            testSubject.Schedule("test path", secondJob.Object, Timeout.Infinite);
            testSubject.Schedule("test path", thirdJob.Object, Timeout.Infinite);

            var firstToken = getFirstToken();
            var secondToken = getSecondToken();
            var thirdToken = getThirdToken();

            firstToken.Should().NotBeEquivalentTo(secondToken);
            secondToken.Should().NotBeEquivalentTo(thirdToken);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Sequential_EachJobIsFinished()
        {
            var firstJob = SetupJobAction(out var getFirstToken);
            var secondJob = SetupJobAction(out var getSecondToken);
            var thirdJob = SetupJobAction(out var getThirdToken);

            testSubject.Schedule("test path", firstJob.Object, Timeout.Infinite);

            var firstToken = getFirstToken();
            firstToken.IsCancellationRequested.Should().BeFalse();

            testSubject.Schedule("test path", secondJob.Object, Timeout.Infinite);

            var secondToken = getSecondToken();
            secondToken.IsCancellationRequested.Should().BeFalse();

            testSubject.Schedule("test path", thirdJob.Object, Timeout.Infinite);

            var thirdToken = getThirdToken();
            thirdToken.IsCancellationRequested.Should().BeFalse();
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Parallel_EachJobIsCalled()
        {
            var firstJob = SetupJobAction(out var getFirstToken, sleepInMiliseconds: 300);
            var secondJob = SetupJobAction(out var getSecondToken, sleepInMiliseconds: 200);
            var thirdJob = SetupJobAction(out var getThirdToken, sleepInMiliseconds: 100);

            var firstJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", firstJob.Object, Timeout.Infinite));
            var secondJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", secondJob.Object, Timeout.Infinite));
            var thirdJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", thirdJob.Object, Timeout.Infinite));

            Task.WaitAll(firstJobTask, secondJobTask, thirdJobTask);

            var firstToken = getFirstToken();
            firstJob.Verify(x => x(firstToken), Times.Once);

            var secondToken = getSecondToken();
            secondJob.Verify(x => x(secondToken), Times.Once);

            var thirdToken = getThirdToken();
            thirdJob.Verify(x => x(thirdToken), Times.Once);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Parallel_EachCallHasNewToken()
        {
            var firstJob = SetupJobAction(out var getFirstToken, sleepInMiliseconds: 300);
            var secondJob = SetupJobAction(out var getSecondToken, sleepInMiliseconds: 200);
            var thirdJob = SetupJobAction(out var getThirdToken, sleepInMiliseconds: 100);

            var firstJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", firstJob.Object, Timeout.Infinite));
            var secondJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", secondJob.Object, Timeout.Infinite));
            var thirdJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", thirdJob.Object, Timeout.Infinite));

            Task.WaitAll(firstJobTask, secondJobTask, thirdJobTask);

            var firstToken = getFirstToken();
            var secondToken = getSecondToken();
            var thirdToken = getThirdToken();

            firstToken.Should().NotBeEquivalentTo(secondToken);
            secondToken.Should().NotBeEquivalentTo(thirdToken);
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Parallel_PreviousJobIsCancelledAndOnlyLatestIsFinished()
        {
            var firstJob = SetupJobAction(out var getFirstToken, sleepInMiliseconds: 300);
            var secondJob = SetupJobAction(out var getSecondToken, sleepInMiliseconds: 100);
            var thirdJob = SetupJobAction(out var getThirdToken, sleepInMiliseconds: 200);

            var firstJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", firstJob.Object, Timeout.Infinite));
            var secondJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", secondJob.Object, Timeout.Infinite));
            var thirdJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", thirdJob.Object, Timeout.Infinite));

            Task.WaitAll(firstJobTask, secondJobTask, thirdJobTask);

            var tokens = new List<CancellationToken>
            {
                getFirstToken(),
                getSecondToken(),
                getThirdToken()
            };

            tokens.Count(x => !x.IsCancellationRequested).Should().Be(1, "Only one token should not be cancelled");
        }

        [TestMethod]
        public void Schedule_MultipleCalls_Parallel_JobIdIsNotCaseSensitive()
        {
            var firstJob = SetupJobAction(out var getFirstToken, sleepInMiliseconds: 300);
            var secondJob = SetupJobAction(out var getSecondToken, sleepInMiliseconds: 100);

            var firstJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test path", firstJob.Object, Timeout.Infinite));
            var secondJobTask = Task.Factory.StartNew(() => testSubject.Schedule("test PATH", secondJob.Object, Timeout.Infinite));

            Task.WaitAll(firstJobTask, secondJobTask);

            var tokens = new List<CancellationToken>
            {
                getFirstToken(),
                getSecondToken(),
            };

            tokens.Count(x => !x.IsCancellationRequested).Should().Be(1, "Only one token should not be cancelled");
        }

        private static Mock<Action<CancellationToken>> SetupJobAction(out Func<CancellationToken> getCreatedToken, int? sleepInMiliseconds = null)
        {
            CancellationToken cancellationToken;
            var action = new Mock<Action<CancellationToken>>();
            action
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

            return action;
        }
    }

    internal static class CancellationTokenExtensions
    {
        public static bool IsTimedOut(this CancellationToken token)
        {
            var fieldInfo = typeof(CancellationToken).GetField("m_source", BindingFlags.NonPublic | BindingFlags.Instance);
            var source = fieldInfo.GetValue(token);

            if (source is ExtendedCancellationTokenSource extendedTokenSource)
            {
                return !extendedTokenSource.IsCancelledExplicitly;
            }

            return false;
        }
    }
}
