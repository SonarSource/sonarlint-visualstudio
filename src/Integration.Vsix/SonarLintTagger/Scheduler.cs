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
        private readonly IDictionary<string, WeakReference<ExtendedCancellationTokenSource>> jobs;

        [ImportingConstructor]
        public Scheduler()
        {
            // Slow memory leak: each unique jobId will add a new entry to the dictionary. Entries for completed jobs are not removed
            jobs = new Dictionary<string, WeakReference<ExtendedCancellationTokenSource>>(StringComparer.OrdinalIgnoreCase);
        }

        public void Schedule(string jobId, Action<CancellationToken> action, int timeoutInMilliseconds)
        {
            var newTokenSource = IssueToken(jobId);

            newTokenSource.CancelAfter(timeoutInMilliseconds);

            action(newTokenSource.Token);
            // The job might be running asynchronously so we don't know when to dispose the CancellationTokenSources, and have to rely on weak-refs and garbage collection to do it for us
        }

        private ExtendedCancellationTokenSource IssueToken(string jobId)
        {
            lock (jobs)
            {
                CancelPreviousJob(jobId);

                var newTokenSource = new ExtendedCancellationTokenSource();
                jobs[jobId] = new WeakReference<ExtendedCancellationTokenSource>(newTokenSource);

                return newTokenSource;
            }
        }

        private void CancelPreviousJob(string jobId)
        {
            if (jobs.ContainsKey(jobId) && jobs[jobId].TryGetTarget(out var tokenSource))
            {
                tokenSource.Cancel(throwOnException: false);
                tokenSource.Dispose();
            }
        }
    }   
}
