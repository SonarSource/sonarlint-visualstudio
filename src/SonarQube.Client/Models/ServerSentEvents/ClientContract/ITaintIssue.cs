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

using System;
using System.Collections.Generic;

namespace SonarQube.Client.Models.ServerSentEvents.ClientContract
{
    public interface ITaintIssue
    {
        string Key { get; }
        string RuleKey { get; }
        DateTimeOffset CreationDate { get; }
        SonarQubeIssueSeverity Severity { get; }
        SonarQubeIssueType Type { get; }
        ILocation MainLocation { get; }
        IFlow[] Flows { get; }
        string Context { get; }
        Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> DefaultImpacts { get; }
    }

    public interface IFlow
    {
        ILocation[] Locations { get; }
    }

    public interface ILocation
    {
        string FilePath { get; }
        string Message { get; }
        ITextRange TextRange { get; }
    }

    public interface ITextRange
    {
        int StartLine { get; }
        int StartLineOffset { get; }
        int EndLine { get; }
        int EndLineOffset { get; }
        string Hash { get; }
    }

    internal class TaintIssue : ITaintIssue
    {
        public TaintIssue(
            string key,
            string ruleKey,
            DateTimeOffset creationDate,
            SonarQubeIssueSeverity severity,
            SonarQubeIssueType type,
            Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> defaultImpacts,
            Location mainLocation,
            Flow[] flows,
            string context)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            RuleKey = ruleKey ?? throw new ArgumentNullException(nameof(ruleKey));
            CreationDate = creationDate;
            Severity = severity;
            Type = type;
            DefaultImpacts = defaultImpacts;
            MainLocation = mainLocation ?? throw new ArgumentNullException(nameof(mainLocation));
            Flows = flows ?? throw new ArgumentNullException(nameof(flows));
            Context = context;
        }

        public string Key { get; }
        public string RuleKey { get; }
        public DateTimeOffset CreationDate { get; }
        public SonarQubeIssueSeverity Severity { get; }
        public SonarQubeIssueType Type { get; }
        public Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> DefaultImpacts { get; }
        public ILocation MainLocation { get; }
        public IFlow[] Flows { get; }
        public string Context { get; }
    }

    internal class Flow : IFlow
    {
        public Flow(Location[] locations)
        {
            Locations = locations ?? throw new ArgumentNullException(nameof(locations));
        }

        public ILocation[] Locations { get; }
    }

    internal class Location : ILocation
    {
        public Location(string filePath, string message, TextRange textRange)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            TextRange = textRange;
        }

        public string FilePath { get; }
        public string Message { get; }
        public ITextRange TextRange { get; }
    }

    internal class TextRange : ITextRange
    {
        public TextRange(int startLine, int startLineOffset, int endLine, int endLineOffset, string hash)
        {
            StartLine = startLine;
            StartLineOffset = startLineOffset;
            EndLine = endLine;
            EndLineOffset = endLineOffset;
            Hash = hash;
        }

        public int StartLine { get; }
        public int StartLineOffset { get; }
        public int EndLine { get; }
        public int EndLineOffset { get; }
        public string Hash { get; }
    }
}
