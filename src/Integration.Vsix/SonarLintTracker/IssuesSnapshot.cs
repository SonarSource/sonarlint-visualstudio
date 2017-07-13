/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System.Globalization;
using Sonarlint;
using System;
using System.Threading;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class IssuesSnapshot : WpfTableEntriesSnapshotBase
    {
        private readonly string projectName;
        private readonly string filePath;
        private readonly int versionNumber;
        private readonly string SonarLintVersion;

        private readonly IList<IssueMarker> issueMarkers;
        private readonly IReadOnlyCollection<IssueMarker> readonlyIssueMarkers;
        public IEnumerable<IssueMarker> IssueMarkers => readonlyIssueMarkers;

        public IssuesSnapshot NextSnapshot;

        internal IssuesSnapshot(string projectName, string filePath, int versionNumber, IEnumerable<IssueMarker> issueMarkers)
        {
            this.projectName = projectName;
            this.filePath = filePath;
            this.versionNumber = versionNumber;
            this.issueMarkers = new List<IssueMarker>(issueMarkers);
            this.readonlyIssueMarkers = new ReadOnlyCollection<IssueMarker>(this.issueMarkers);
            SonarLintVersion = FileVersionInfo.GetVersionInfo(typeof(IssuesSnapshot).Assembly.Location).FileVersion;
            // Remove last version digit since rule website URL is only based on the 3 first digits
            SonarLintVersion = SonarLintVersion.Substring(0, SonarLintVersion.LastIndexOf('.'));
        }

        public override int Count => this.issueMarkers.Count;

        public override int VersionNumber => this.versionNumber;

        public override bool TryGetValue(int index, string columnName, out object content)
        {
            if (index < 0 || this.issueMarkers.Count <= index)
            {
                content = null;
                return false;
            }

            switch (columnName)
            {
                case StandardTableKeyNames.DocumentName:
                    content = filePath;
                    return true;

                case StandardTableKeyNames.Line:
                    content = this.issueMarkers[index].Span.Start.GetContainingLine().LineNumber;
                    return true;

                case StandardTableKeyNames.Column:
                    var position = this.issueMarkers[index].Span.Start;
                    var line = position.GetContainingLine();
                    content = position.Position - line.Start.Position;
                    return true;

                case StandardTableKeyNames.Text:
                    content = this.issueMarkers[index].Issue.Message;
                    return true;

                case StandardTableKeyNames.ErrorSeverity:
                    // TODO use correct icon type depending on severity
                    content = __VSERRORCATEGORY.EC_WARNING;
                    return true;

                case StandardTableKeyNames.BuildTool:
                    content = "SonarLint";
                    return true;

                case StandardTableKeyNames.ErrorCode:
                    content = this.issueMarkers[index].Issue.RuleKey;
                    return true;

                case StandardTableKeyNames.ErrorRank:
                    content = ErrorRank.Other;
                    return true;

                case StandardTableKeyNames.ErrorCategory:
                    content = ToString(this.issueMarkers[index].Issue.Type);
                    return true;

                case StandardTableKeyNames.ErrorCodeToolTip:
                    content = $"Open description of rule {this.issueMarkers[index].Issue.RuleKey}";
                    return true;

                case StandardTableKeyNames.HelpLink:
                    string ruleKey = this.issueMarkers[index].Issue.RuleKey;
                    string ruleId = ruleKey.Substring(ruleKey.IndexOf(':') + 1);
                    string repo = ruleKey.Substring(0, ruleKey.IndexOf(':'));
                    string language = GetLanguage(repo);
                    string url = $"http://vs.sonarlint.org/rules/index.html#sonarLintVersion={SonarLintVersion}&ruleId={ruleId}";
                    if (language != null)
                    {
                        url += $"&language={Uri.EscapeDataString(language)}";
                    }
                    content = url;
                    return true;

                case StandardTableKeyNames.ProjectName:
                    content = projectName;
                    return true;
                default:
                    content = null;
                    return false;
            }
        }

        private string GetLanguage(string repo)
        {
            switch (repo)
            {
                case "javascript": return "JavaScript";
                case "c": return "C";
                case "cpp": return "C++";
                default: return null;
            }
        }

        private object ToString(Issue.Types.Type type)
        {
            switch (type)
            {
                case Issue.Types.Type.Vulnerability: return "Vulnerability";
                case Issue.Types.Type.Bug: return "Bug";
                case Issue.Types.Type.CodeSmell:
                default:
                    return "Code Smell";
            }
        }

        public override bool CanCreateDetailsContent(int index)
        {
            // TODO flip to true when detailed description is ready
            return false;
        }

        public override bool TryCreateDetailsStringContent(int index, out string content)
        {
            // TODO use the detailed description
            content = this.issueMarkers[index].Issue.Message;
            return true;
        }
    }
}
