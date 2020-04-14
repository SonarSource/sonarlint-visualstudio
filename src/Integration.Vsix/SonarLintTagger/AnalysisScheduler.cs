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
    [Export(typeof(IAnalysisScheduler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AnalysisScheduler : IAnalysisScheduler
    {
        private readonly ILogger logger;
        private readonly IDictionary<string, CancellationTokenSource> analysisJobs;

        [ImportingConstructor]
        public AnalysisScheduler(ILogger logger)
        {
            this.logger = logger;
            this.analysisJobs = new Dictionary<string, CancellationTokenSource>();
        }

        public void Schedule(string filePath, Action<CancellationToken> analyzeAction)
        {
            using var newAnalysisToken = IssueToken(filePath);

            analyzeAction(newAnalysisToken.Token);

            lock (analysisJobs)
            {
                if (analysisJobs[filePath] == newAnalysisToken)
                {
                    analysisJobs[filePath] = null;
                }
            }
        }

        private CancellationTokenSource IssueToken(string filePath)
        {
            lock (analysisJobs)
            {
                CancelPreviousAnalysis(filePath);

                var newAnalysis = new CancellationTokenSource();
                analysisJobs[filePath] = newAnalysis;

                return newAnalysis;
            }
        }

        private void CancelPreviousAnalysis(string filePath)
        {
            if (analysisJobs.ContainsKey(filePath))
            {
                logger.WriteLine($"Cancelled analysis for {filePath}");

                analysisJobs[filePath]?.Cancel(throwOnFirstException: false);

                analysisJobs[filePath]?.Dispose();
                analysisJobs[filePath] = null;
            }
        }
    }
}
