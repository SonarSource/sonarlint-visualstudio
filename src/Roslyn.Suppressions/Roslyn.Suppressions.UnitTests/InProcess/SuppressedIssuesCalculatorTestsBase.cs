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
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarQube.Client.Models;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess;

public class SuppressedIssuesCalculatorTestsBase
{
    internal const string RoslynSettingsKey = "my solution";
    internal IRoslynSettingsFileStorage RoslynSettingsFileStorage;
    internal ILogger Logger;

    protected readonly SonarQubeIssue CsharpIssueSuppressed = CreateSonarQubeIssue("csharpsquid:S111");
    protected readonly SonarQubeIssue VbNetIssueSuppressed = CreateSonarQubeIssue("vbnet:S222");
    protected readonly SonarQubeIssue CppIssueSuppressed = CreateSonarQubeIssue("cpp:S333");
    protected readonly SonarQubeIssue UnknownRepoIssue = CreateSonarQubeIssue("xxx:S444");
    protected readonly SonarQubeIssue InvalidRepoKeyIssue = CreateSonarQubeIssue("xxxS555");
    protected readonly SonarQubeIssue NoRuleIdIssue = CreateSonarQubeIssue("xxx:");
    protected readonly SonarQubeIssue CsharpIssueNotSuppressed = CreateSonarQubeIssue("csharpsquid:S333", isSuppressed: false);
    protected readonly SonarQubeIssue VbNetIssueNotSuppressed = CreateSonarQubeIssue("vbnet:S444", isSuppressed: false);

    internal static void VerifyExpectedSuppressions(SuppressedIssue[] actualSuppressions, SonarQubeIssue[] expectedSuppressions)
    {
        actualSuppressions.Should().HaveCount(expectedSuppressions.Length);
        actualSuppressions.Should().BeEquivalentTo(expectedSuppressions.Select(IssueConverter.Convert));
    }

    protected void MockExistingSuppressionsOnSettingsFile(params SonarQubeIssue[] existingIssues) =>
        RoslynSettingsFileStorage.Get(Arg.Any<string>()).Returns(existingIssues.Length == 0
            ? RoslynSettings.Empty
            : new RoslynSettings { SonarProjectKey = RoslynSettingsKey, Suppressions = existingIssues.Select(IssueConverter.Convert) });
}
