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

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess;

[TestClass]
public class SuppressedIssuesCalculatorTests : SuppressedIssuesCalculatorTestsBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        RoslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        Logger = Substitute.For<ILogger>();
        Logger.ForContext(Arg.Any<string[]>()).Returns(Logger);
    }

    [TestMethod]
    public void AllSuppressedIssuesCalculator_IssuesAreConvertedAndFiltered()
    {
        var testSubject = CreateAllSuppressedIssuesCalculator([
            CsharpIssueSuppressed, // C# issue
            VbNetIssueSuppressed, // VB issue
            CppIssueSuppressed, // C++ issue - ignored
            UnknownRepoIssue, // unrecognised repo - ignored
            InvalidRepoKeyIssue, // invalid repo key - ignored
            NoRuleIdIssue // invalid repo key (no rule id) - ignored
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [CsharpIssueSuppressed, VbNetIssueSuppressed]);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    [TestMethod]
    public void AllSuppressedIssuesCalculator_OnlySuppressedIssuesAreInSettings()
    {
        var testSubject = CreateAllSuppressedIssuesCalculator([
            CsharpIssueSuppressed,
            VbNetIssueSuppressed,
            CsharpIssueNotSuppressed,
            VbNetIssueNotSuppressed,
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [CsharpIssueSuppressed, VbNetIssueSuppressed]);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    private AllSuppressedIssuesCalculator CreateAllSuppressedIssuesCalculator(IEnumerable<SonarQubeIssue> sonarQubeIssues) => new(Logger, sonarQubeIssues);
}
