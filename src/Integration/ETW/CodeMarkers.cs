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
    [EventSource(Name = "sonarlint.integration")]
    public sealed class CodeMarkers : EventSource
    {
        public static readonly CodeMarkers Instance = new CodeMarkers();

        public static class Keywords
        {
            // Must be powers of 2 so they can be combined bitwise
            public const EventKeywords General = (EventKeywords)1;
            public const EventKeywords Binding = (EventKeywords)2;
        }

        #region Binding: 1000-1999

        private const int ErrorListControllerStartId = 1000;
        private const int ErrorListControllerEndId = 1001;

        private const int UnboundProjectsFinderStartId = 1002;
        private const int UnboundProjectsFinderEndId = 1003;

        private const int CheckProjectBindingStartId = 1004;
        private const int CheckProjectBindingEndId = 1005;

        private const int CSharpVBIsBindingRequiredStartId = 1006;
        private const int CSharpVBIsBindingRequiredEndId = 1007;

        private const int GetProjectRuleSetsDeclarationsStartId = 1008;
        private const int GetProjectRuleSetsDeclarationsEndId = 1009;

        [Event(ErrorListControllerStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void ErrorListControllerStart() => Write(ErrorListControllerStartId);

        [Event(ErrorListControllerEndId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void ErrorListControllerEnd() => Write(ErrorListControllerEndId);

        [Event(UnboundProjectsFinderStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void UnboundProjectFinderStart() => Write(UnboundProjectsFinderStartId);

        [Event(UnboundProjectsFinderEndId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void UnboundProjectFinderEnd() => Write(UnboundProjectsFinderEndId);

        [Event(CheckProjectBindingStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CheckProjectBindingStart(string projectName) => Write(CheckProjectBindingStartId, projectName);

        [Event(CheckProjectBindingEndId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CheckProjectBindingEnd() => Write(CheckProjectBindingEndId);

        [Event(CSharpVBIsBindingRequiredStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CSharpVBProjectIsBindingRequiredStart(string projectName) => Write(CSharpVBIsBindingRequiredStartId);

        [Event(CSharpVBIsBindingRequiredEndId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void CSharpVBIsBindingRequiredEnd() => Write(CSharpVBIsBindingRequiredEndId);

        [Event(GetProjectRuleSetsDeclarationsStartId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetProjectRuleSetsDeclarationsStart(string projectName) => Write(GetProjectRuleSetsDeclarationsStartId);

        [Event(GetProjectRuleSetsDeclarationsEndId, Level = EventLevel.Informational, Keywords = Keywords.Binding)]
        public void GetProjectRuleSetsDeclarationsEnd() => Write(GetProjectRuleSetsDeclarationsEndId);

        #endregion

        private void Write(int id)
        {
            if (IsEnabled())
            {
                WriteEvent(id);
            }
        }

        private void Write(int id, string text)
        {
            if (IsEnabled())
            {
                WriteEvent(id, text);
            }
        }
    }
}
