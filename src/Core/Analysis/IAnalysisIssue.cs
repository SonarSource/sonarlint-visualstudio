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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core
{
    public interface IAnalysisIssue
    {
        string RuleKey { get; }

        AnalysisIssueSeverity Severity { get; }

        AnalysisIssueType Type { get; }

        int StartLine { get; }

        int EndLine { get; }

        int StartLineOffset { get; }

        int EndLineOffset { get; }

        string Message { get; }

        string FilePath { get; }
    }

    public enum AnalysisIssueSeverity
    {
        Blocker,
        Critical,
        Major,
        Minor,
        Info,
    }

    public enum AnalysisIssueType
    {
        CodeSmell,
        Bug,
        Vulnerability,
        Hotspot
    }

    public class AnalysisIssue : IAnalysisIssue
    {
        public AnalysisIssue(
            string ruleKey, AnalysisIssueSeverity severity, AnalysisIssueType type,
            string message, string filePath,
            int startLine, int endLine,
            int startLineOffset, int endLineOffset
            )
        {
            RuleKey = ruleKey;
            Severity = severity;
            Type = type;
            StartLine = startLine;
            StartLineOffset = startLineOffset;
            EndLine = endLine;
            EndLineOffset = endLineOffset;
            FilePath = filePath;
            Message = message;
        }

        public string RuleKey { get; }

        public AnalysisIssueSeverity Severity { get; }

        public AnalysisIssueType Type { get; }

        public int StartLine { get; }

        public int EndLine { get; }

        public int StartLineOffset { get; }

        public int EndLineOffset { get; }

        public string Message { get; }

        public string FilePath { get; }
    }
}
