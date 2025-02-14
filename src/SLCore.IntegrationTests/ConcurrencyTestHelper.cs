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

using EnvDTE;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

public static class ConcurrencyTestHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static Task WaitForTaskWithTimeout(
        Task task,
        string taskName,
        ILogger logger = null,
        TimeSpan? timeout = null) =>
        WaitForTaskWithTimeout(_ => task, taskName, logger, timeout);

    public static async Task WaitForTaskWithTimeout(
        Func<CancellationToken, Task> func,
        string taskName,
        ILogger logger,
        TimeSpan? timeout = null)
    {
        var cts = new CancellationTokenSource();
        var task = func(cts.Token);
        var whenAny = await Task.WhenAny(task, Task.Delay(timeout ?? DefaultTimeout, cts.Token));
        if (whenAny != task)
        {
            const string someTask = "some task";
            var name = taskName ?? someTask;
            logger.WriteLine($"timeout reached for {name}");
            await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30), cts.Token));
            cts.Cancel();
            Assert.Fail($"timeout reached for {name} at {DateTime.Now.TimeOfDay:G}");
        }
    }
}
