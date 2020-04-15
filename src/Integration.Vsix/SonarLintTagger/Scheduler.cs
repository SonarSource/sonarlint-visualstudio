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
using System.ComponentModel.Composition;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IScheduler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Scheduler : IScheduler
    {
        private readonly ILogger logger;
        private readonly IDictionary<string, WeakReference<CancellationTokenSource>> jobs;

        [ImportingConstructor]
        public Scheduler(ILogger logger)
        {
            this.logger = logger;
            this.jobs = new Dictionary<string, WeakReference<CancellationTokenSource>>(StringComparer.OrdinalIgnoreCase);
        }

        public void Schedule(string jobId, Action<CancellationToken> action)
        {
            var newTokenSource = IssueToken(jobId);
            action(newTokenSource.Token);
        }
        private CancellationTokenSource IssueToken(string jobId)
        {
            lock (jobs)
            {
                CancelPreviousJob(jobId);
                var newTokenSource = new CancellationTokenSource();
                jobs[jobId] = new WeakReference<CancellationTokenSource>(newTokenSource);
                return newTokenSource;
            }
        }
        private void CancelPreviousJob(string jobId)
        {
            if (jobs.ContainsKey(jobId))
            {
                logger.WriteLine($"Cancelled job for {jobId}");
                if (jobs[jobId].TryGetTarget(out var tokenSource))
                {
                    tokenSource.Cancel(throwOnFirstException: false);
                    tokenSource.Dispose();
                }
            }
        }
    }
}
