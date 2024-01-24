/*
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

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace SonarLint.VisualStudio.Core.ETW
{
    [EventSource(Name = "SonarSource-SonarLint-VS-Integration")]
    [ExcludeFromCodeCoverage] // boilerplate. The ETW provider will log errors if the manifest is invalid
    public sealed class CodeMarkers : EventSource
    {
        public static readonly CodeMarkers Instance = new CodeMarkers();

        public static class Keywords
        {
            // Must be powers of 2 so they can be combined bitwise
            public const EventKeywords General = (EventKeywords)1;
            public const EventKeywords Binding = (EventKeywords)2;
            public const EventKeywords Analysis = (EventKeywords)4;
            public const EventKeywords CFamily = (EventKeywords)8;
            public const EventKeywords RoslynSuppression = (EventKeywords)16;
            public const EventKeywords WebClient = (EventKeywords)32;
            public const EventKeywords Threading = (EventKeywords)64;
        }

        #region Binding: 1000-1999

        private const int ErrorListControllerProcessStartId = 1000;
        private const int ErrorListControllerProcessStopId = 1001;

        private const int GetProjectRuleSetsDeclarationsStartId = 1009;
        private const int GetProjectRuleSetsDeclarationsStopId = 1010;

        private const int GetMatchingBranchStartId = 1011;
        private const int GetMatchingBranchStopId = 1012;

        private const int GetDistanceStartId = 1013;
        private const int GetDistanceStopId = 1014;

        [Event(ErrorListControllerProcessStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void ErrorListControllerProcessStart() => Write(ErrorListControllerProcessStartId);

        [Event(ErrorListControllerProcessStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void ErrorListControllerProcessStop() => Write(ErrorListControllerProcessStopId);

        [Event(GetProjectRuleSetsDeclarationsStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetProjectRuleSetsDeclarationsStart(string projectName) => Write(GetProjectRuleSetsDeclarationsStartId, projectName);

        [Event(GetProjectRuleSetsDeclarationsStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetProjectRuleSetsDeclarationsStop() => Write(GetProjectRuleSetsDeclarationsStopId);

        [Event(GetMatchingBranchStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetMatchingBranchStart(string projectKey) => Write(GetMatchingBranchStartId, projectKey);

        [Event(GetMatchingBranchStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetMatchingBranchStop() => Write(GetMatchingBranchStopId);

        [Event(GetDistanceStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetDistanceStart(string branchName) => Write(GetDistanceStartId, branchName);

        [Event(GetDistanceStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetDistanceStop() => Write(GetDistanceStopId);

        #endregion

        #region CFamily: 2000-2999

        private const int CFamilyHandleMessageStartId = 2000;
        private const int CFamilyHandleMessageStopId = 2001;
        private const int CFamilyConvertIssueStartId = 2002;
        private const int CFamilyConvertIssueStopId = 2003;
        private const int CFamilyConvertIssueFileLoadedId = 2004;
        private const int CFamilyConvertIssueFileAlreadyLoadedId = 2005;

        [Event(CFamilyHandleMessageStartId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyHandleMessageStart(string fileName) => Write(CFamilyHandleMessageStartId, fileName);

        [Event(CFamilyHandleMessageStopId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyHandleMessageStop() => Write(CFamilyHandleMessageStopId);

        [Event(CFamilyConvertIssueStartId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueStart(string fileName) => Write(CFamilyConvertIssueStartId, fileName);

        [Event(CFamilyConvertIssueStopId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueStop() => Write(CFamilyConvertIssueStopId);

        [Event(CFamilyConvertIssueFileLoadedId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueFileLoaded(string filePath) => Write(CFamilyConvertIssueFileLoadedId, filePath);

        [Event(CFamilyConvertIssueFileAlreadyLoadedId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueFileAlreadyLoaded(string filePath) => Write(CFamilyConvertIssueFileAlreadyLoadedId, filePath);

        #endregion

        #region Roslyn Suppressions 3000-3999

        private const int ReportSuppressionsStartId = 3000;
        private const int ReportSuppressionsStopId = 3001;
      
        private const int FileStorageGetStartId = 3002;
        private const int FileStorageGetStopId = 3003;

        private const int FileStorageUpdateStartId = 3004;
        private const int FileStorageUpdateStopId = 3005;

        private const int FileWatcherInvalidateStartId = 3006;
        private const int FileWatcherInvalidateStopId = 3007;

        private const int FileSynchronizerUpdateStartId = 3008;
        private const int FileSynchronizerUpdateStopId = 3009;


        [Event(ReportSuppressionsStartId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void ReportSuppressionsStart() => Write(ReportSuppressionsStartId);
        
        [Event(ReportSuppressionsStopId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void ReportSuppressionsStop(string mode, int suppressionCount) => Write(ReportSuppressionsStopId, mode, suppressionCount);

        [Event(FileStorageGetStartId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileStorageGetStart() => Write(FileStorageGetStartId);

        [Event(FileStorageGetStopId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileStorageGetStop() => Write(FileStorageGetStopId);

        [Event(FileStorageUpdateStartId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileStorageUpdateStart() => Write(FileStorageUpdateStartId);

        [Event(FileStorageUpdateStopId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileStorageUpdateStop() => Write(FileStorageUpdateStopId);

        [Event(FileWatcherInvalidateStartId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileWatcherInvalidateStart(string type) => Write(FileWatcherInvalidateStartId, type);

        [Event(FileWatcherInvalidateStopId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileWatcherInvalidateStop(string type) => Write(FileWatcherInvalidateStopId, type);

        [Event(FileSynchronizerUpdateStartId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileSynchronizerUpdateStart() => Write(FileSynchronizerUpdateStartId);

        [Event(FileSynchronizerUpdateStopId, Level = EventLevel.Informational, Keywords = Keywords.RoslynSuppression)]
        public void FileSynchronizerUpdateStop() => Write(FileSynchronizerUpdateStopId);

        #endregion

        #region Web call: 4000-4999

        private const int WebClientCallStartId = 4000;
        private const int WebClinetCallStopId = 4001;

        [Event(WebClientCallStartId, Level = EventLevel.Informational, Keywords = Keywords.WebClient)]
        public void WebClientCallStart(string requestName) => Write(WebClientCallStartId, requestName);

        [Event(WebClinetCallStopId, Level = EventLevel.Informational, Keywords = Keywords.WebClient)]
        public void WebClientCallStop(string requestName) => Write(WebClinetCallStopId, requestName);

        #endregion

        #region Threading: 5000-5999

        private const int Threading_RunOnBackgroundThread_StartId = 5000;
        private const int Threading_RunOnBackgroundThread_StopId = 5001;
        private const int Threading_RunOnBackgroundThread_SwitchingToBackgroundId = 5002;
        private const int Threading_RunOnBackgroundThread_ExecutingCallId = 5003;
        private const int Threading_RunOnBackgroundThread_SwitchingToUIId = 5004;

        [Event(Threading_RunOnBackgroundThread_StartId, Level = EventLevel.Informational, Keywords = Keywords.Threading)]
        public void Threading_RunOnBackgroundThread_Start() => Write(Threading_RunOnBackgroundThread_StartId);

        [Event(Threading_RunOnBackgroundThread_StopId, Level = EventLevel.Informational, Keywords = Keywords.Threading)]
        public void Threading_RunOnBackgroundThread_Stop() => Write(Threading_RunOnBackgroundThread_StopId);

        [Event(Threading_RunOnBackgroundThread_SwitchingToBackgroundId, Level = EventLevel.Informational, Keywords = Keywords.Threading)]
        public void Threading_RunOnBackgroundThread_SwitchingToBackground() => Write(Threading_RunOnBackgroundThread_SwitchingToBackgroundId);

        [Event(Threading_RunOnBackgroundThread_ExecutingCallId, Level = EventLevel.Informational, Keywords = Keywords.Threading)]
        public void Threading_RunOnBackgroundThread_ExecutingCall() => Write(Threading_RunOnBackgroundThread_ExecutingCallId);

        [Event(Threading_RunOnBackgroundThread_SwitchingToUIId, Level = EventLevel.Informational, Keywords = Keywords.Threading)]
        public void Threading_RunOnBackgroundThread_SwitchingToUI() => Write(Threading_RunOnBackgroundThread_SwitchingToUIId);

        #endregion

        [NonEvent]
        private void Write(int id)
        {
            if (IsEnabled())
            {
                WriteEvent(id);
            }
        }

        [NonEvent]
        private void Write(int id, string text)
        {
            if (IsEnabled())
            {
                WriteEvent(id, text);
            }
        }

        [NonEvent]
        private void Write(int id, params object[] data)
        {
            if (IsEnabled())
            {
                WriteEvent(id, data);
            }
        }
    }
}
