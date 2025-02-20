/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;

internal abstract class SuppressedIssuesCalculatorBase : ISuppressedIssuesCalculator
{
    public abstract IEnumerable<SuppressedIssue> GetSuppressedIssuesOrNull(string roslynSettingsKey);

    protected static SuppressedIssue[] GetRoslynSuppressedIssues(IEnumerable<SonarQubeIssue> sonarQubeIssues)
    {
        var suppressionsToAdd = sonarQubeIssues
            .Where(x => x.IsResolved)
            .Select(IssueConverter.Convert)
            .Where(x => x.RoslynLanguage != RoslynLanguage.Unknown && !string.IsNullOrEmpty(x.RoslynRuleId))
            .ToArray();
        return suppressionsToAdd;
    }
}

internal class AllSuppressedIssuesCalculator(ILogger logger, IEnumerable<SonarQubeIssue> sonarQubeIssues) : SuppressedIssuesCalculatorBase
{
    public override IEnumerable<SuppressedIssue> GetSuppressedIssuesOrNull(string roslynSettingsKey)
    {
        logger.LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
        return GetRoslynSuppressedIssues(sonarQubeIssues);
    }
}

internal class NewSuppressedIssuesCalculator(ILogger logger, IRoslynSettingsFileStorage roslynSettingsFileStorage, IEnumerable<SonarQubeIssue> newSonarQubeIssues) : SuppressedIssuesCalculatorBase
{
    public override IEnumerable<SuppressedIssue> GetSuppressedIssuesOrNull(string roslynSettingsKey)
    {
        logger.LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);

        var suppressedIssuesInFile = roslynSettingsFileStorage.Get(roslynSettingsKey)?.Suppressions;
        var suppressedIssuesToAdd = GetRoslynSuppressedIssues(newSonarQubeIssues);
        if (suppressedIssuesInFile is null)
        {
            // if the settings do not exist on disk, add all the new suppressed issues
            return suppressedIssuesToAdd;
        }

        var suppressedIssuesToAddNotExistingInFile = suppressedIssuesToAdd.Where(newIssue => suppressedIssuesInFile.All(existing => !newIssue.AreSame(existing)));
        return suppressedIssuesInFile.Concat(suppressedIssuesToAddNotExistingInFile);
    }
}

internal class SuppressedIssuesRemovedCalculator(ILogger logger, IRoslynSettingsFileStorage roslynSettingsFileStorage, IEnumerable<string> resolvedIssueServerKeys) : SuppressedIssuesCalculatorBase
{
    public override IEnumerable<SuppressedIssue> GetSuppressedIssuesOrNull(string roslynSettingsKey)
    {
        var suppressedIssuesInFile = roslynSettingsFileStorage.Get(roslynSettingsKey)?.Suppressions?.ToList();
        var resolvedIssues = suppressedIssuesInFile?.Where(existingIssue => resolvedIssueServerKeys.Any(x => existingIssue.IssueServerKey == x)).ToList();
        if (resolvedIssues == null || !resolvedIssues.Any())
        {
            // nothing to be done if no issue from file was resolved
            return null;
        }

        logger.LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
        return suppressedIssuesInFile.Except(resolvedIssues);
    }
}
