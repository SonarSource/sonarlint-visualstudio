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
public class ResettableOneShotTimerTests
{
    [TestMethod]
    public async Task ResettableOneShotTimer_SmokeTest()
    {
        var timerTimeSpan = TimeSpan.FromMilliseconds(100);
        var testSubject = new ResettableOneShotTimer(timerTimeSpan);
        var eventHandler = Substitute.For<EventHandler>();
        testSubject.Elapsed += eventHandler;

        testSubject.Reset();
        await Task.Delay(2 * (int)timerTimeSpan.TotalMilliseconds);

        eventHandler.ReceivedWithAnyArgs(1).Invoke(default, default);
    }

    [TestMethod]
    public void ResettableOneShotTimer_Dispose_DisposesTimer()
    {
        var timerTimeSpan = TimeSpan.FromMilliseconds(50);
        var testSubject = new ResettableOneShotTimer(timerTimeSpan);

        testSubject.Dispose();

        var act = () => testSubject.Reset();
        act.Should().Throw<ObjectDisposedException>();
    }
}
