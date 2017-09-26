/*
 * SonarQube Client
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

using System;

namespace SonarQube.Client.Models
{
    public enum SonarQubeIssueResolutionState
    {
        Open,
        Fixed,
        FalsePositive,
        WontFix
    }

    public class SonarQubeIssue
    {
        public string Key { get; }
        public int Line { get; }
        public SonarQubeIssueResolutionState ResolutionState { get; }
        public string Hash { get; }

        public SonarQubeIssue(string key, int line, SonarQubeIssueResolutionState resolution, string hash)
        {
            Key = key;
            Line = line;
            ResolutionState = resolution;
            Hash = hash;
        }

        public static SonarQubeIssue FromDto(ServerIssue issue)
        {
            return new SonarQubeIssue(issue.Key, issue.Line, ParseResolutionState(issue.Resolution), issue.Checksum);
        }

        public static SonarQubeIssueResolutionState ParseResolutionState(string resolution)
        {
            switch (resolution)
            {
                case "OPEN":
                    return SonarQubeIssueResolutionState.Open;
                case "WONTFIX":
                    return SonarQubeIssueResolutionState.WontFix;
                case "FALSE-POSITIVE":
                    return SonarQubeIssueResolutionState.FalsePositive;
                case "FIXED":
                    return SonarQubeIssueResolutionState.Fixed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution));
            }
        }
    }
}
