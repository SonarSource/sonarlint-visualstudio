﻿/*
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

using System;
using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    internal class AnalysisStatusNotifier : IAnalysisStatusNotifier
    {
        private readonly string analyzerName;
        private readonly string filePath;
        private readonly Guid? analysisId;
        private readonly IStatusBarNotifier statusBarNotifier;
        private readonly ILogger logger;

        public AnalysisStatusNotifier(string analyzerName, string filePath, Guid? analysisId, IStatusBarNotifier statusBarNotifier, ILogger logger)
        {
            this.analyzerName = analyzerName;
            this.filePath = filePath;
            this.analysisId = analysisId;
            this.statusBarNotifier = statusBarNotifier;
            this.logger = logger;
        }

        public void AnalysisStarted()
        {
            Log(AnalysisStrings.MSG_AnalysisStarted, filePath, analysisId);

            Notify(AnalysisStrings.Notifier_AnalysisStarted, true);
        }

        public void AnalysisProgressed(
            int issueCount,
            string findingType,
            bool isIntermediate) =>
            Log(AnalysisStrings.MSG_FoundIssues, issueCount, findingType, filePath, analysisId, !isIntermediate);

        public void AnalysisFinished(TimeSpan analysisTime)
        {
            Log(AnalysisStrings.MSG_AnalysisComplete, filePath, analysisId, Math.Round(analysisTime.TotalSeconds, 3));

            Notify(AnalysisStrings.Notifier_AnalysisFinished, false);
        }

        public void AnalysisCancelled()
        {
            Log(AnalysisStrings.MSG_AnalysisAborted, filePath, analysisId);

            Notify("", false);
        }

        public void AnalysisFailed(Exception ex)
        {
            AnalysisFailed(ex.ToString());
        }

        public void AnalysisFailed(string failureMessage)
        {
            Log(AnalysisStrings.MSG_AnalysisFailed, filePath, analysisId, failureMessage);

            Notify(AnalysisStrings.Notifier_AnalysisFailed, false);
        }

        public void AnalysisNotReady(string reason)
        {
            Log(AnalysisStrings.MSG_AnalysisNotReady, filePath, analysisId, reason);

            Notify("", false);
        }

        private void Log(string messageFormat, params object[] args)
        {
            logger.WriteLine($"[{analyzerName}] " + messageFormat, args);
        }

        private void Notify(string messageFormat, bool showSpinner)
        {
            var fileName = Path.GetFileName(filePath);
            var message = string.Format(messageFormat, fileName);
            statusBarNotifier.Notify(message, showSpinner);
        }
    }
}
