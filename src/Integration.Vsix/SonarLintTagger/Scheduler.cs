﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IScheduler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Scheduler : IScheduler
    {
        private readonly IDictionary<string, WeakReference<CancellationTokenSource>> jobs;
        private readonly Action<CancellationToken> onExplicitCancel;

        [ImportingConstructor]
        public Scheduler()
            : this(null)
        {
        }

        internal Scheduler(Action<CancellationToken> onExplicitCancel)
        {
            this.onExplicitCancel = onExplicitCancel;
            // Slow memory leak: each unique jobId will add a new entry to the dictionary. Entries for completed jobs are not removed
            jobs = new Dictionary<string, WeakReference<CancellationTokenSource>>(StringComparer.OrdinalIgnoreCase);
        }

        public void Schedule(string jobId, Action<CancellationToken> action, int timeoutInMilliseconds)
        {
            var newCancellationToken = IssueToken(jobId, timeoutInMilliseconds);

            action(newCancellationToken);
            // The job might be running asynchronously so we don't know when to dispose the CancellationTokenSources, and have to rely on weak-refs and garbage collection to do it for us
        }

        private CancellationToken IssueToken(string jobId, int timeoutInMilliseconds)
        {
            lock (jobs)
            {
                CancelPreviousJob(jobId);

                var newTokenSource = new CancellationTokenSource();
                newTokenSource.CancelAfter(timeoutInMilliseconds);

                jobs[jobId] = new WeakReference<CancellationTokenSource>(newTokenSource);

                return newTokenSource.Token;
            }
        }

        private void CancelPreviousJob(string jobId)
        {
            if (jobs.ContainsKey(jobId) && jobs[jobId].TryGetTarget(out var tokenSource))
            {
                onExplicitCancel?.Invoke(tokenSource.Token);
                tokenSource.Cancel(throwOnFirstException: false);
                tokenSource.Dispose();
            }
        }
    }   
}
