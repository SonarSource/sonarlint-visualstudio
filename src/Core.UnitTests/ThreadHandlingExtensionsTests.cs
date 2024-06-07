/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.UnitTests;

[TestClass]
public class ThreadHandlingExtensionsTests
{
    [TestMethod]
    public async Task RunOnBackgroundThread_SyncVoidMethod_UsesMainRunOnBackgroundThread()
    {
        var threadHandling = Substitute.For<IThreadHandling>();

        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(async info => await info.Arg<Func<Task<int>>>()());

        var called = false;
        
        await ThreadHandlingExtensions.RunOnBackgroundThread(threadHandling, () => called = true);

        called.Should().BeTrue();
        threadHandling.Received().RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
    }
    
    [TestMethod]
    public async Task RunOnBackgroundThread_TaskReturningMethod_UsesMainRunOnBackgroundThread()
    {
        var threadHandling = Substitute.For<IThreadHandling>();

        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(async info => await info.Arg<Func<Task<int>>>()());

        var called = false;
        
        await ThreadHandlingExtensions.RunOnBackgroundThread(threadHandling, () =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
        threadHandling.Received().RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
    }
    
    [TestMethod]
    public async Task RunOnBackgroundThread_AsyncEmptyTaskMethod_UsesMainRunOnBackgroundThread()
    {
        var threadHandling = Substitute.For<IThreadHandling>();

        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(async info => await info.Arg<Func<Task<int>>>()());

        var called = false;
        
        await ThreadHandlingExtensions.RunOnBackgroundThread(threadHandling, async () => called = true);

        called.Should().BeTrue();
        threadHandling.Received().RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
    }
}
