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

using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class TaskExecutorWithDebounceTest
{
    private TaskExecutorWithDebounce testSubject;
    private NoOpThreadHandler threadHandling;
    private IResettableOneShotTimer timer;
    private TimeSpan debounceInterval = TimeSpan.FromSeconds(123);

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        timer = Substitute.For<IResettableOneShotTimer>();
        testSubject = new TaskExecutorWithDebounce(timer, threadHandling);
    }

    [TestMethod]
    public void Debounce_TimerNotRaised_DoesNotExecuteAction()
    {
        var action = Substitute.For<Action<CancellationToken>>();

        testSubject.Debounce(action, debounceInterval);

        action.DidNotReceiveWithAnyArgs().Invoke(default);
        timer.Received().Reset(debounceInterval);
    }

    [TestMethod]
    public void Debounce_NoActionSet_DoesNotThrow()
    {
        var act = () => timer.Elapsed += Raise.Event();

        act.Should().NotThrow();
        testSubject.IsScheduled.Should().BeFalse();
    }

    [TestMethod]
    public void Debounce_ExecutesTaskWithDebounce()
    {
        var action = Substitute.For<Action<CancellationToken>>();
        testSubject.Debounce(action, debounceInterval);

        testSubject.IsScheduled.Should().BeTrue();
        timer.Elapsed += Raise.Event();
        testSubject.IsScheduled.Should().BeFalse();

        Received.InOrder(() =>
        {
            timer.Reset(debounceInterval);
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            action.Invoke(Arg.Any<CancellationToken>());
        });
    }

    [TestMethod]
    public void Debounce_MultipleTimes_UpdatesWithLatestState()
    {
        var action1 = Substitute.For<Action<CancellationToken>>();
        var action2 = Substitute.For<Action<CancellationToken>>();
        var action3 = Substitute.For<Action<CancellationToken>>();
        testSubject.Debounce(action1, debounceInterval);
        testSubject.Debounce(action2, debounceInterval);
        testSubject.Debounce(action3, debounceInterval);

        timer.Elapsed += Raise.Event();

        action1.DidNotReceiveWithAnyArgs().Invoke(default);
        action2.DidNotReceiveWithAnyArgs().Invoke(default);
        action3.Received().Invoke(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void Debounce_CancelsPreviousActionWhenExecuting()
    {
        var token = CancellationToken.None;
        void CaptureCancellation(CancellationToken ct) => token = ct;

        var mockAction = Substitute.For<Action<CancellationToken>>();

        testSubject.Debounce(CaptureCancellation, debounceInterval);
        timer.Elapsed += Raise.Event();
        testSubject.Debounce(mockAction, debounceInterval);

        token.IsCancellationRequested.Should().BeFalse();
        timer.Elapsed += Raise.Event();
        token.IsCancellationRequested.Should().BeTrue();

        mockAction.Received().Invoke(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void Debounce_MultipleTriggers_ActionOnlyExecutedOnce()
    {
        var action = Substitute.For<Action<CancellationToken>>();
        testSubject.Debounce(action, debounceInterval);

        timer.Elapsed += Raise.Event();
        timer.Elapsed += Raise.Event();
        timer.Elapsed += Raise.Event();

        action.Received(1).Invoke(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void Dispose_DisposesTimer()
    {
        testSubject.Dispose();

        timer.Received(1).Dispose();
        timer.Received(1).Elapsed -= Arg.Any<EventHandler>();
    }
}
