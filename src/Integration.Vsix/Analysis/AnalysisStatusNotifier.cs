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
using System.ComponentModel.Composition;
using System.IO;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [Export(typeof(IAnalysisStatusNotifier))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AnalysisStatusNotifier : IAnalysisStatusNotifier
    {
        private readonly IStatusBarNotifier statusBarNotifier;
        private readonly ILogger logger;

        [ImportingConstructor]
        public AnalysisStatusNotifier(IStatusBarNotifier statusBarNotifier, ILogger logger)
        {
            this.statusBarNotifier = statusBarNotifier;
            this.logger = logger;
        }

        public void AnalysisStarted(string filePath)
        {
            logger.WriteLine(AnalysisStrings.MSG_AnalysisStarted, filePath);

            Notify(AnalysisStrings.Notifier_AnalysisStarted, filePath, true);
        }

        public void AnalysisFinished(string filePath, int issueCount, TimeSpan analysisTime)
        {
            logger.WriteLine(AnalysisStrings.MSG_AnalysisComplete, filePath, Math.Round(analysisTime.TotalSeconds, 3));
            logger.WriteLine(AnalysisStrings.MSG_FoundIssues, issueCount, filePath);

            Notify(AnalysisStrings.Notifier_AnalysisFinished, filePath, false);
        }

        public void AnalysisCancelled(string filePath)
        {
            logger.WriteLine(AnalysisStrings.MSG_AnalysisAborted, filePath);
            
            Notify("", "", false);
        }

        public void AnalysisFailed(string filePath, Exception ex)
        {
            logger.WriteLine(AnalysisStrings.MSG_AnalysisFailed, filePath, ex.ToString());

            Notify(AnalysisStrings.Notifier_AnalysisFailed, filePath, false);
        }

        private void Notify(string messageFormat, string filePath, bool showSpinner)
        {
            var fileName = Path.GetFileName(filePath);
            var message = string.Format(messageFormat, fileName);
            statusBarNotifier.Notify(message, showSpinner);
        }
    }
}
