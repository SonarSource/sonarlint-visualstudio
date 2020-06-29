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
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public interface IScheduler
    {
        /// <summary>
        /// Issues a cancellation token for a given job and fires cancellation request for the previous job for same jobId.
        /// </summary>
        /// <param name="jobId">Unique key based on which previous job is cancelled</param>
        /// <param name="action">Job action</param>
        /// <param name="timeoutInMilliseconds">number of milliseconds after which the job should be cancelled</param>
        void Schedule(string jobId, Action<CancellationToken> action, int timeoutInMilliseconds);
    }
}
