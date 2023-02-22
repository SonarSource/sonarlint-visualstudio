﻿/*
 * SonarQube Client
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

using System.Collections.Generic;
namespace SonarQube.Client.Models
{
    public class SonarQubeIssue
    {
        private static readonly IReadOnlyList<IssueFlow> EmptyFlows = new List<IssueFlow>().AsReadOnly();

        public SonarQubeIssue(string filePath, string hash, int? line, string message, string moduleKey, string ruleId,
            bool isResolved, List<IssueFlow> flows)
        {
            FilePath = filePath?.Trim('/', '\\');
            Hash = hash;
            Line = line;
            Message = message;
            ModuleKey = moduleKey;
            RuleId = ruleId;
            IsResolved = isResolved;
            Flows = flows ?? EmptyFlows;
        }

        public string FilePath { get; }
        public string Hash { get; }
        public int? Line { get; }
        public string Message { get; }
        public string ModuleKey { get; }
        public string RuleId { get; }
        public bool IsResolved { get; }
        public IReadOnlyList<IssueFlow> Flows { get; }
    }

    public class IssueFlow
    {
        private static readonly IReadOnlyList<IssueLocation> EmptyLocations = new List<IssueLocation>().AsReadOnly();

        public IssueFlow(List<IssueLocation> locations)
        {
            Locations = locations ?? EmptyLocations;
        }

        public IReadOnlyList<IssueLocation> Locations { get; }
    }

    public class IssueLocation
    {
        public IssueLocation(string filePath, string moduleKey, IssueTextRange textRange, string message)
        {
            FilePath = filePath;
            ModuleKey = moduleKey;
            TextRange = textRange;
            Message = message;
        }

        public string FilePath { get; }
        public string ModuleKey { get; }
        public IssueTextRange TextRange { get; }
        public string Message { get; }
    }

    public class IssueTextRange
    {
        public IssueTextRange(int startLine, int endLine, int startOffset, int endOffset)
        {
            StartLine = startLine;
            EndLine = endLine;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public int StartLine { get; }
        public int EndLine { get; }
        public int StartOffset { get; }
        public int EndOffset { get; }
    }
}
