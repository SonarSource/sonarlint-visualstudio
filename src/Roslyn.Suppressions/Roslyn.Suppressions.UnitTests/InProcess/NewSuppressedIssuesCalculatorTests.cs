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

[TestClass]
public class NewSuppressedIssuesCalculatorTests : SuppressedIssuesCalculatorTestsBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        RoslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        Logger = Substitute.For<ILogger>();
        Logger.ForContext(Arg.Any<string[]>()).Returns(Logger);
    }

    [TestMethod]
    public void CreateNewSuppressedIssuesCalculator_IssueDoNotExist_IssuesAreConvertedAndFiltered()
    {
        var testSubject = CreateNewSuppressedIssuesCalculator([
            CsharpIssueSuppressed, // C# issue
            VbNetIssueSuppressed, // VB issue
            CppIssueSuppressed, // C++ issue - ignored
            UnknownRepoIssue, // unrecognised repo - ignored
            InvalidRepoKeyIssue, // invalid repo key - ignored
            NoRuleIdIssue // invalid repo key (no rule id) - ignored
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [CsharpIssueSuppressed, VbNetIssueSuppressed]);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void CreateNewSuppressedIssuesCalculator_IssueDoNotExist_OnlySuppressedIssuesAreInSettings()
    {
        MockExistingSuppressionsOnSettingsFile();
        var testSubject = CreateNewSuppressedIssuesCalculator([
            CsharpIssueSuppressed,
            VbNetIssueSuppressed,
            CsharpIssueNotSuppressed,
            VbNetIssueNotSuppressed,
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [CsharpIssueSuppressed, VbNetIssueSuppressed]);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void CreateNewSuppressedIssuesCalculator_IssuesExist_UpdatesCorrectly()
    {
        var newSonarQubeIssues = new[] { CsharpIssueSuppressed, VbNetIssueSuppressed };
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);
        var testSubject = CreateNewSuppressedIssuesCalculator(newSonarQubeIssues);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], newSonarQubeIssues);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void CreateNewSuppressedIssuesCalculator_TwoNewIssuesAdded_DoesNotRemoveExistingOnes()
    {
        var newSonarQubeIssues = new[] { CreateSonarQubeIssue("csharpsquid:S666"), CreateSonarQubeIssue("vbnet:S666") };
        var existingIssues = new[] { CsharpIssueSuppressed, VbNetIssueSuppressed, };
        MockExistingSuppressionsOnSettingsFile(existingIssues);
        var testSubject = CreateNewSuppressedIssuesCalculator(newSonarQubeIssues);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        var expectedIssues = existingIssues.Union(newSonarQubeIssues).ToArray();
        VerifyExpectedSuppressions([.. result], expectedIssues);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    private NewSuppressedIssuesCalculator CreateNewSuppressedIssuesCalculator(IEnumerable<SonarQubeIssue> sonarQubeIssues) => new(Logger, RoslynSettingsFileStorage, sonarQubeIssues);
}
