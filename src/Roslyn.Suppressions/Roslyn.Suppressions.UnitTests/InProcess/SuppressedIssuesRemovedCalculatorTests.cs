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
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess;

[TestClass]
public class SuppressedIssuesRemovedCalculatorTests : SuppressedIssuesCalculatorTestsBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        RoslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        Logger = Substitute.For<ILogger>();
        Logger.ForContext(Arg.Any<string[]>()).Returns(Logger);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_IssueDoNotExistInFile_DoesNotUpdateFile()
    {
        MockExistingSuppressionsOnSettingsFile();
        var newSonarQubeIssues = new[]
        {
            CsharpIssueSuppressed.IssueKey, // C# issue
            VbNetIssueSuppressed.IssueKey, // VB issue
            CppIssueSuppressed.IssueKey, // C++ issue - ignored
            UnknownRepoIssue.IssueKey, // unrecognised repo - ignored
            InvalidRepoKeyIssue.IssueKey, // invalid repo key - ignored
            NoRuleIdIssue.IssueKey // invalid repo key (no rule id) - ignored
        };
        var testSubject = CreateSuppressedIssuesRemovedCalculator(newSonarQubeIssues);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        result.Should().BeNull();
        RoslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        Logger.DidNotReceive().LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_IssueKeysExistInFile_RemovesIssues()
    {
        var newSonarQubeIssues = new[] { CsharpIssueSuppressed, VbNetIssueSuppressed };
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);
        var testSubject = CreateSuppressedIssuesRemovedCalculator(newSonarQubeIssues.Select(x => x.IssueKey).ToArray());

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], []);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_OneNewIssueResolved_DoesNotRemoveExistingOne()
    {
        var existingIssues = new[] { CsharpIssueSuppressed, VbNetIssueSuppressed, };
        MockExistingSuppressionsOnSettingsFile(existingIssues);
        var testSubject = CreateSuppressedIssuesRemovedCalculator([CsharpIssueSuppressed.IssueKey]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [VbNetIssueSuppressed]);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_MultipleIssuesWithSameIssueServerKeyExistsInFile_RemovesThemAll()
    {
        var existingIssues = new[] { CsharpIssueSuppressed, CreateSonarQubeIssue(issueKey: CsharpIssueSuppressed.IssueKey), CreateSonarQubeIssue(issueKey: CsharpIssueSuppressed.IssueKey) };
        MockExistingSuppressionsOnSettingsFile(existingIssues);

        var testSubject = CreateSuppressedIssuesRemovedCalculator([CsharpIssueSuppressed.IssueKey]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], []);
        Logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    private SuppressedIssuesRemovedCalculator CreateSuppressedIssuesRemovedCalculator(IEnumerable<string> serverIssueKeys) => new(Logger, RoslynSettingsFileStorage, serverIssueKeys);
}
