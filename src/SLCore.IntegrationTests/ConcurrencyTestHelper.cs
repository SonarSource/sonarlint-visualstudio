﻿/*
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

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

public static class ConcurrencyTestHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public static async Task WaitForTaskWithTimeout(Task task, string taskName, TimeSpan? timeout = null)
    {
        var duration = Stopwatch.StartNew();
        var taskOrTimeout = await Task.WhenAny(task, Task.Delay(timeout ?? DefaultTimeout));
        if (taskOrTimeout != task)
        {
            Assert.Fail($"Task [{taskName}] timed out after {duration.Elapsed.TotalSeconds}s");
        }
    }
}
