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

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

public static class ConcurrencyTestHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public static Task WaitForTaskWithTimeout(Task task, TimeSpan? timeout = null) =>
        WaitForTaskWithTimeout(_ => task, timeout);
    
    public static async Task WaitForTaskWithTimeout(Func<CancellationToken, Task> func, TimeSpan? timeout = null)
    {
        var cts = new CancellationTokenSource();
        var task = func(cts.Token);
        var whenAny = await Task.WhenAny(task, Task.Delay(timeout ?? DefaultTimeout, cts.Token));
        if (whenAny != task)
        {
            cts.Cancel();
            Assert.Fail("timeout reached");
        }
    }
}
