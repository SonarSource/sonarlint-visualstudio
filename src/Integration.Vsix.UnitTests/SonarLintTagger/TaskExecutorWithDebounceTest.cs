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
        var action = Substitute.For<Action>();

        testSubject.Debounce(action);

        action.DidNotReceive().Invoke();
        timer.Received().Reset();
    }

    [TestMethod]
    public void Debounce_NoActionSet_DoesNotThrow()
    {
        var act = () => timer.Elapsed += Raise.Event();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Debounce_ExecutesTaskWithDebounce()
    {
        var action = Substitute.For<Action>();
        testSubject.Debounce(action);

        timer.Elapsed += Raise.Event();

        Received.InOrder(() =>
        {
            timer.Reset();
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            action.Invoke();
        });
    }

    [TestMethod]
    public void Debounce_MultipleTimes_UpdatesWithLatestState()
    {
        var action1 = Substitute.For<Action>();
        var action2 = Substitute.For<Action>();
        var action3 = Substitute.For<Action>();
        testSubject.Debounce(action1);
        testSubject.Debounce(action2);
        testSubject.Debounce(action3);

        timer.Elapsed += Raise.Event();

        action1.DidNotReceive().Invoke();
        action2.DidNotReceive().Invoke();
        action3.Received().Invoke();
    }

    [TestMethod]
    public void Debounce_MultipleTriggers_ActionOnlyExecutedOnce()
    {
        var action = Substitute.For<Action>();
        testSubject.Debounce(action);

        timer.Elapsed += Raise.Event();
        timer.Elapsed += Raise.Event();
        timer.Elapsed += Raise.Event();

        action.Received(1).Invoke();
    }
}
