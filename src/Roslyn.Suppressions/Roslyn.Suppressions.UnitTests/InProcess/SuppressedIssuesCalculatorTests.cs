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
public class SuppressedIssuesCalculatorTests
{
    private const string RoslynSettingsKey = "my solution";
    private IRoslynSettingsFileStorage roslynSettingsFileStorage;
    private ILogger logger;

    private readonly SonarQubeIssue csharpIssueSuppressed = CreateSonarQubeIssue("csharpsquid:S111");
    private readonly SonarQubeIssue vbNetIssueSuppressed = CreateSonarQubeIssue("vbnet:S222");
    private readonly SonarQubeIssue cppIssueSuppressed = CreateSonarQubeIssue("cpp:S333");
    private readonly SonarQubeIssue unknownRepoIssue = CreateSonarQubeIssue("xxx:S444");
    private readonly SonarQubeIssue invalidRepoKeyIssue = CreateSonarQubeIssue("xxxS555");
    private readonly SonarQubeIssue noRuleIdIssue = CreateSonarQubeIssue("xxx:");
    private readonly SonarQubeIssue csharpIssueNotSuppressed = CreateSonarQubeIssue("csharpsquid:S333", isSuppressed: false);
    private readonly SonarQubeIssue vbNetIssueNotSuppressed = CreateSonarQubeIssue("vbnet:S444", isSuppressed: false);

    [TestInitialize]
    public void TestInitialize()
    {
        roslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
    }

    [TestMethod]
    public void AllSuppressedIssuesCalculator_IssuesAreConvertedAndFiltered()
    {
        var testSubject = CreateAllSuppressedIssuesCalculator([
            csharpIssueSuppressed, // C# issue
            vbNetIssueSuppressed, // VB issue
            cppIssueSuppressed, // C++ issue - ignored
            unknownRepoIssue, // unrecognised repo - ignored
            invalidRepoKeyIssue, // invalid repo key - ignored
            noRuleIdIssue // invalid repo key (no rule id) - ignored
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [csharpIssueSuppressed, vbNetIssueSuppressed]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_OnlySuppressedIssuesAreInSettings()
    {
        var testSubject = CreateAllSuppressedIssuesCalculator([
            csharpIssueSuppressed,
            vbNetIssueSuppressed,
            csharpIssueNotSuppressed,
            vbNetIssueNotSuppressed,
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [csharpIssueSuppressed, vbNetIssueSuppressed]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_IssueDoNotExist_IssuesAreConvertedAndFiltered()
    {
        var testSubject = CreateNewSuppressedIssuesCalculator([
            csharpIssueSuppressed, // C# issue
            vbNetIssueSuppressed, // VB issue
            cppIssueSuppressed, // C++ issue - ignored
            unknownRepoIssue, // unrecognised repo - ignored
            invalidRepoKeyIssue, // invalid repo key - ignored
            noRuleIdIssue // invalid repo key (no rule id) - ignored
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [csharpIssueSuppressed, vbNetIssueSuppressed]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_IssueDoNotExist_OnlySuppressedIssuesAreInSettings()
    {
        MockExistingSuppressionsOnSettingsFile();
        var testSubject = CreateNewSuppressedIssuesCalculator([
            csharpIssueSuppressed,
            vbNetIssueSuppressed,
            csharpIssueNotSuppressed,
            vbNetIssueNotSuppressed,
        ]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [csharpIssueSuppressed, vbNetIssueSuppressed]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_IssuesExist_UpdatesCorrectly()
    {
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);
        var testSubject = CreateNewSuppressedIssuesCalculator(newSonarQubeIssues);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], newSonarQubeIssues);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_TwoNewIssuesAdded_DoesNotRemoveExistingOnes()
    {
        var newSonarQubeIssues = new[] { CreateSonarQubeIssue("csharpsquid:S666"), CreateSonarQubeIssue("vbnet:S666") };
        var existingIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, };
        MockExistingSuppressionsOnSettingsFile(existingIssues);
        var testSubject = CreateNewSuppressedIssuesCalculator(newSonarQubeIssues);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        var expectedIssues = existingIssues.Union(newSonarQubeIssues).ToArray();
        VerifyExpectedSuppressions([.. result], expectedIssues);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_IssueDoNotExistInFile_DoesNotUpdateFile()
    {
        MockExistingSuppressionsOnSettingsFile();
        var newSonarQubeIssues = new[]
        {
            csharpIssueSuppressed.IssueKey, // C# issue
            vbNetIssueSuppressed.IssueKey, // VB issue
            cppIssueSuppressed.IssueKey, // C++ issue - ignored
            unknownRepoIssue.IssueKey, // unrecognised repo - ignored
            invalidRepoKeyIssue.IssueKey, // invalid repo key - ignored
            noRuleIdIssue.IssueKey // invalid repo key (no rule id) - ignored
        };
        var testSubject = CreateSuppressedIssuesRemovedCalculator(newSonarQubeIssues);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        result.Should().BeNull();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        logger.DidNotReceive().LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_IssueKeysExistInFile_RemovesIssues()
    {
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);
        var testSubject = CreateSuppressedIssuesRemovedCalculator(newSonarQubeIssues.Select(x => x.IssueKey).ToArray());

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], []);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_OneNewIssueResolved_DoesNotRemoveExistingOne()
    {
        var existingIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, };
        MockExistingSuppressionsOnSettingsFile(existingIssues);
        var testSubject = CreateSuppressedIssuesRemovedCalculator([csharpIssueSuppressed.IssueKey]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], [vbNetIssueSuppressed]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesRemovedCalculator_MultipleIssuesWithSameIssueServerKeyExistsInFile_RemovesThemAll()
    {
        var existingIssues = new[] { csharpIssueSuppressed, CreateSonarQubeIssue(issueKey: csharpIssueSuppressed.IssueKey), CreateSonarQubeIssue(issueKey: csharpIssueSuppressed.IssueKey) };
        MockExistingSuppressionsOnSettingsFile(existingIssues);

        var testSubject = CreateSuppressedIssuesRemovedCalculator([csharpIssueSuppressed.IssueKey]);

        var result = testSubject.GetSuppressedIssuesOrNull(RoslynSettingsKey);

        VerifyExpectedSuppressions([.. result], []);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    private AllSuppressedIssuesCalculator CreateAllSuppressedIssuesCalculator(IEnumerable<SonarQubeIssue> sonarQubeIssues) => new(logger, sonarQubeIssues);

    private NewSuppressedIssuesCalculator CreateNewSuppressedIssuesCalculator(IEnumerable<SonarQubeIssue> sonarQubeIssues) => new(logger, roslynSettingsFileStorage, sonarQubeIssues);

    private SuppressedIssuesRemovedCalculator CreateSuppressedIssuesRemovedCalculator(IEnumerable<string> serverIssueKeys) => new(logger, roslynSettingsFileStorage, serverIssueKeys);

    private static void VerifyExpectedSuppressions(SuppressedIssue[] actualSuppressions, SonarQubeIssue[] expectedSuppressions)
    {
        actualSuppressions.Should().HaveCount(expectedSuppressions.Length);
        actualSuppressions.Should().BeEquivalentTo(expectedSuppressions.Select(IssueConverter.Convert));
    }

    private void MockExistingSuppressionsOnSettingsFile(params SonarQubeIssue[] existingIssues) =>
        roslynSettingsFileStorage.Get(Arg.Any<string>()).Returns(existingIssues.Length == 0
            ? RoslynSettings.Empty
            : new RoslynSettings { SonarProjectKey = RoslynSettingsKey, Suppressions = existingIssues.Select(IssueConverter.Convert) });
}
