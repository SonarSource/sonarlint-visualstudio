/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    internal class AnalysisStatusNotifier(
        string[] filePaths,
        IStatusBarNotifier statusBarNotifier,
        ILogger logger)
        : IAnalysisStatusNotifier
    {
        private readonly string formattedFileNames = string.Join(", ", filePaths.Select(Path.GetFileName));
        private readonly ILogger logger = logger.ForContext(AnalysisStrings.AnalysisLogContext);

        public void AnalysisStarted()
        {
            Log(AnalysisStrings.MSG_AnalysisStarted, formattedFileNames);

            Notify(AnalysisStrings.Notifier_AnalysisStarted, true);
        }

        public void AnalysisProgressed(
            Guid? analysisId,
            int issueCount,
            string findingType,
            bool isIntermediate) =>
            Log(AnalysisStrings.MSG_FoundIssues, analysisId, issueCount, findingType, formattedFileNames, !isIntermediate);

        public void AnalysisFinished(Guid? analysisId, TimeSpan analysisTime)
        {
            Log(AnalysisStrings.MSG_AnalysisComplete, analysisId, formattedFileNames, Math.Round(analysisTime.TotalSeconds, 3));

            Notify(AnalysisStrings.Notifier_AnalysisFinished, false);
        }

        public void AnalysisCancelled(Guid? analysisId)
        {
            Log(AnalysisStrings.MSG_AnalysisAborted, analysisId, formattedFileNames);

            Notify("", false);
        }

        public void AnalysisFailed(Guid? analysisId, Exception ex) => AnalysisFailed(analysisId, ex.ToString());

        public void AnalysisFailed(Guid? analysisId, string failureMessage)
        {
            Log(AnalysisStrings.MSG_AnalysisFailed, analysisId, formattedFileNames, failureMessage);

            Notify(AnalysisStrings.Notifier_AnalysisFailed, false);
        }

        public void AnalysisNotReady(Guid? analysisId, string reason)
        {
            Log(AnalysisStrings.MSG_AnalysisNotReady, analysisId, formattedFileNames, reason);

            Notify("", false);
        }

        private void Log(string messageFormat, params object[] args) => logger.WriteLine(messageFormat, args);

        private void Notify(string messageFormat, bool showSpinner)
        {
            var fileNames = formattedFileNames;
            var message = string.Format(messageFormat, fileNames);
            statusBarNotifier.Notify(message, showSpinner);
        }
    }
}
