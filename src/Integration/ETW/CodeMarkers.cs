/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.Diagnostics.Tracing;

namespace SonarLint.VisualStudio.Integration.ETW
{
    [EventSource(Name = "SonarSource-SonarLint-VS-Integration")]
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
        }

        #region Binding: 1000-1999

        private const int ErrorListControllerProcessStartId = 1000;
        private const int ErrorListControllerProcessStopId = 1001;

        private const int UnboundProjectsFinderStartId = 1002;
        private const int UnboundProjectsFinderStopId = 1003;
        private const int UnboundProjectsFinderBeforeIsBindingRequired = 1004;

        private const int CheckProjectBindingStartId = 1005;
        private const int CheckProjectBindingStopId = 1006;

        private const int CSharpVBIsBindingRequiredStartId = 1007;
        private const int CSharpVBIsBindingRequiredStopId = 1008;

        private const int GetProjectRuleSetsDeclarationsStartId = 1009;
        private const int GetProjectRuleSetsDeclarationsStopId = 1010;

        [Event(ErrorListControllerProcessStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void ErrorListControllerProcessStart() => Write(ErrorListControllerProcessStartId);

        [Event(ErrorListControllerProcessStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void ErrorListControllerProcessStop() => Write(ErrorListControllerProcessStopId);

        [Event(UnboundProjectsFinderStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void UnboundProjectFinderStart() => Write(UnboundProjectsFinderStartId);

        [Event(UnboundProjectsFinderStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void UnboundProjectFinderStop() => Write(UnboundProjectsFinderStopId);

        [Event(UnboundProjectsFinderBeforeIsBindingRequired, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void UnboundProjectFinderBeforeIsBindingRequired() => Write(UnboundProjectsFinderBeforeIsBindingRequired);

        [Event(CheckProjectBindingStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CheckProjectBindingStart(string projectName) => Write(CheckProjectBindingStartId, projectName);

        [Event(CheckProjectBindingStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CheckProjectBindingStop() => Write(CheckProjectBindingStopId);

        [Event(CSharpVBIsBindingRequiredStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CSharpVBProjectIsBindingRequiredStart(string projectName) => Write(CSharpVBIsBindingRequiredStartId, projectName);

        [Event(CSharpVBIsBindingRequiredStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CSharpVBIsBindingRequiredStop() => Write(CSharpVBIsBindingRequiredStopId);

        [Event(GetProjectRuleSetsDeclarationsStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetProjectRuleSetsDeclarationsStart(string projectName) => Write(GetProjectRuleSetsDeclarationsStartId, projectName);

        [Event(GetProjectRuleSetsDeclarationsStopId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetProjectRuleSetsDeclarationsStop() => Write(GetProjectRuleSetsDeclarationsStopId);

        #endregion

        #region Analysis: 2000-2999

        private const int HandleMessageStartId = 2000;
        private const int HandleMessageStopId = 2001;

        [Event(HandleMessageStartId, Level = EventLevel.Informational, Keywords = Keywords.Analysis)]
        public void HandleMessageStart(string fileName) => Write(HandleMessageStartId, fileName);

        [Event(HandleMessageStopId, Level = EventLevel.Informational, Keywords = Keywords.Analysis)]
        public void HandleMessageStop() => Write(HandleMessageStopId);

        #endregion

        #region Analysis: 3000-3999

        private const int CFamilyConvertIssueStartId = 3000;
        private const int CFamilyConvertIssueStopId = 3001;
        private const int CFamilyConvertIssueFileLoadedId = 3002;
        private const int CFamilyConvertIssueFileAlreadyLoadedId = 3003;

        [Event(CFamilyConvertIssueStartId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueStart(string fileName) => Write(CFamilyConvertIssueStartId, fileName);

        [Event(CFamilyConvertIssueStopId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueStop() => Write(CFamilyConvertIssueStopId);

        [Event(CFamilyConvertIssueFileLoadedId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueFileLoaded(string filePath) => Write(CFamilyConvertIssueFileLoadedId, filePath);

        [Event(CFamilyConvertIssueFileAlreadyLoadedId, Level = EventLevel.Informational, Keywords = Keywords.CFamily | Keywords.Analysis)]
        public void CFamilyConvertIssueFileAlreadyLoaded(string filePath) => Write(CFamilyConvertIssueFileAlreadyLoadedId, filePath);

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
    }
}
