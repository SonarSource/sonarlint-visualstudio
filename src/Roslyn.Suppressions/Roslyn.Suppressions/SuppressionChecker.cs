﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    internal interface ISuppressionChecker
    {
        /// <summary>
        /// Returns true if the diagnostic should be suppressed, otherwise false
        /// </summary>
        bool IsSuppressed(Diagnostic reportedDiagnostic, string settingsKey);
    }

    internal class SuppressionChecker : ISuppressionChecker
    {
        private readonly ISettingsCache settingsCache;
        private readonly IChecksumCalculator checksumCalculator;

        public SuppressionChecker(ISettingsCache settingsCache)
            : this(settingsCache, new ChecksumCalculator())
        {
        }

        internal SuppressionChecker(ISettingsCache settingsCache, IChecksumCalculator checksumCalculator)
        {
            this.settingsCache = settingsCache;
            this.checksumCalculator = checksumCalculator;
        }

        public bool IsSuppressed(Diagnostic reportedDiagnostic, string settingsKey)
        {
            // Can't match issues that don't have a location in source
            if (reportedDiagnostic.Location == null || reportedDiagnostic.Location.Kind != LocationKind.SourceFile)
            {
                return false;
            }

            var suppressedIssues = settingsCache.GetSettings(settingsKey);
            if (suppressedIssues == null || !suppressedIssues.Any())
            {
                return false;
            }

            // TODO: consider perf. This initial implementation is very inefficient:
            // * it loops through every issue
            // * the hash of the Roslyn issue could be calculate multiple times.
            bool matchFound = suppressedIssues.Any(x => IsMatch(reportedDiagnostic, x, checksumCalculator));
            return matchFound;
        }

        internal static /* for testing */ bool IsMatch(Diagnostic diagnostic, SonarQubeIssue suppressedIssue, IChecksumCalculator checksumCalculator)
        {
            // Criteria for matching a Roslyn issue to an issue from the server:
            // (1) same issue key
            // (2) same file
            // (3) same line number OR same line hash (to take account of edits in the file since it was analysed)

            // Note: this first implementation is written in a verbose style to make it easier to debug.

            // (1) Id
            if (!StringComparer.OrdinalIgnoreCase.Equals(diagnostic.Id, suppressedIssue.RuleId))
            {
                return false;
            }

            // (2) File
            if (!IsSameFile(diagnostic, suppressedIssue))
            {
                return false;
            }

            // (3) Location - line matches
            var roslynLineSpan = diagnostic.Location.GetLineSpan();
            if (IsSameLine(roslynLineSpan, suppressedIssue.TextRange))
            {
                return true;
            }

            // (3) Location - hash (most expensive check)
            var syntaxTree = diagnostic.Location.SourceTree;
            // TODO: check why the existing RoslynLiveIssueFactory.Create is using the EndLine of the issue
            // rather than the StartLine
            var lineText = syntaxTree.GetText().Lines[roslynLineSpan.EndLinePosition.Line].ToString();
            var roslynHash = checksumCalculator.Calculate(lineText);

            if (StringComparer.Ordinal.Equals(roslynHash, suppressedIssue.Hash))
            {
                return true;
            }

            return false;
        }

        private static bool IsSameFile(Diagnostic diagnostic, SonarQubeIssue suppressedIssue) =>
            diagnostic.Location.SourceTree?.FilePath.EndsWith(suppressedIssue.FilePath, StringComparison.OrdinalIgnoreCase)
            ?? false;

        private static bool IsSameLine(FileLinePositionSpan roslynLineSpan, IssueTextRange sonarTextRange)
        {
            // Special case: file-level issues
            var roslynFileLevelIssue = IsRoslynFileLevelIssue(roslynLineSpan);
            if (sonarTextRange == null || roslynFileLevelIssue)
            {
                // File-level issues can only match other file-level issues 
                return (sonarTextRange == null && roslynFileLevelIssue);
            }


            // Roslyn lines are 0-based, SonarQube lines are 1-based
            return sonarTextRange.StartLine == roslynLineSpan.StartLinePosition.Line + 1;
        }

        private static bool IsRoslynFileLevelIssue(FileLinePositionSpan lineSpan) => lineSpan.StartLinePosition.Line == 0 &&
                           lineSpan.StartLinePosition.Character == 0 &&
                           lineSpan.EndLinePosition.Line == 0 &&
                           lineSpan.EndLinePosition.Character == 0;
    }
}
