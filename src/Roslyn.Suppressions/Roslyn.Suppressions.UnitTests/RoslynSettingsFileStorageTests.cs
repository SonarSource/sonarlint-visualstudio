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

using System.IO.Abstractions;
using Moq;
using Newtonsoft.Json;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Roslyn.Suppressions.Resources;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests;

[TestClass]
public class RoslynSettingsFileStorageTests
{
    private const string SolutionName = "a solution name";
    private IFile file;
    private IFileSystem fileSystem;
    private TestLogger logger;
    private RoslynSettingsFileStorage testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = new TestLogger();
        file = Substitute.For<IFile>();
        fileSystem = Substitute.For<IFileSystem>();
        testSubject = new RoslynSettingsFileStorage(logger, fileSystem);

        MockFileSystem();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynSettingsFileStorage, IRoslynSettingsFileStorage>(
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void Update_HasIssues_IssuesWrittenToFile()
    {
        var settings = new RoslynSettings { SonarProjectKey = "projectKey", Suppressions = new[] { CreateIssue("issue1") } };

        testSubject.Update(settings, SolutionName);

        CheckFileWritten(settings, SolutionName);
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void Get_HasIssues_IssuesReadFromFile()
    {
        var issue1 = CreateIssue("key1");
        var issue2 = CreateIssue("key2");
        var settings = new RoslynSettings { SonarProjectKey = "projectKey", Suppressions = new[] { issue1, issue2 } };
        MockFileReadAllText(settings);

        var actual = testSubject.Get("projectKey");

        var issuesGotten = actual.Suppressions.ToList();

        file.Received(1).ReadAllText(GetFilePath("projectKey"));
        logger.AssertNoOutputMessages();

        issuesGotten.Count.Should().Be(2);
        issuesGotten[0].RoslynRuleId.Should().Be(issue1.RoslynRuleId);
        issuesGotten[1].RoslynRuleId.Should().Be(issue2.RoslynRuleId);
    }

    [TestMethod]
    public void Update_SolutionNameHasInvalidChars_InvalidCharsReplaced()
    {
        var settings = new RoslynSettings { SonarProjectKey = "project:key" };

        testSubject.Update(settings, "my:solution");

        CheckFileWritten(settings, "my_solution");
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void Update_ErrorOccuredWhenWritingFile_ErrorIsLogged()
    {
        var settings = new RoslynSettings { SonarProjectKey = "projectKey" };
        file.When(x => x.WriteAllText(Arg.Any<string>(), Arg.Any<string>())).Do(x => throw new Exception("Test Exception"));

        testSubject.Update(settings, "any");

        logger.AssertOutputStrings("[Roslyn Suppressions] Error writing settings for project projectKey. Issues suppressed on the server may not be suppressed in the IDE. Error: Test Exception");
    }

    [TestMethod]
    public void Get_ErrorOccuredWhenWritingFile_ErrorIsLoggedAndReturnsNull()
    {
        file.ReadAllText(GetFilePath("projectKey")).Throws(new Exception("Test Exception"));

        var actual = testSubject.Get("projectKey");

        logger.AssertOutputStrings("[Roslyn Suppressions] Error loading settings for project projectKey. Issues suppressed on the server will not be suppressed in the IDE. Error: Test Exception");
        actual.Should().BeNull();
    }

    [TestMethod]
    public void Update_HasNoIssues_FileWritten()
    {
        var settings = new RoslynSettings { SonarProjectKey = "projectKey", Suppressions = Enumerable.Empty<SuppressedIssue>() };

        testSubject.Update(settings, "mySolution1");

        CheckFileWritten(settings, "mySolution1");
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void Get_HasNoIssues_ReturnsEmpty()
    {
        var settings = new RoslynSettings { SonarProjectKey = "projectKey", Suppressions = Enumerable.Empty<SuppressedIssue>() };
        MockFileReadAllText(settings);

        var actual = testSubject.Get("projectKey");

        var issuesGotten = actual.Suppressions.ToList();
        file.Received(1).ReadAllText(GetFilePath("projectKey"));
        logger.AssertNoOutputMessages();
        issuesGotten.Count.Should().Be(0);
    }

    [TestMethod]
    public void Get_FileDoesNotExist_ErrorIsLoggedAndReturnsNull()
    {
        MockFileSystem(false);

        var actual = testSubject.Get("projectKey");

        logger.AssertOutputStrings(
            "[Roslyn Suppressions] Error loading settings for project projectKey. Issues suppressed on the server will not be suppressed in the IDE. Error: Settings File was not found");
        actual.Should().BeNull();
    }

    [TestMethod] // Regression test for SLVS-2946
    public void SaveAndLoadSettings()
    {
        string serializedText = null;
        CreateSaveAndReloadFile(serializedText);
        var projectKey = "projectKey";
        var original = new RoslynSettings
        {
            SonarProjectKey = projectKey,
            Suppressions =
            [
                CreateIssue("rule1", "path1", null), // null line number
                CreateIssue("RULE2", "PATH2", 111, null, RoslynLanguage.VB) // null hash
            ]
        };

        // Act
        testSubject.Update(original, "any");

        var reloaded = testSubject.Get(projectKey);

        reloaded.SonarProjectKey.Should().Be(projectKey);
        reloaded.Suppressions.Should().NotBeNull();
        reloaded.Suppressions.Count().Should().Be(2);

        var firstSuppression = reloaded.Suppressions.First();
        firstSuppression.RoslynRuleId.Should().Be("rule1");
        firstSuppression.FilePath.Should().Be("path1");
        firstSuppression.RoslynIssueLine.Should().BeNull();
        firstSuppression.Hash.Should().Be("hash");
        firstSuppression.RoslynLanguage.Should().Be(RoslynLanguage.CSharp);

        var secondSuppression = reloaded.Suppressions.Last();
        secondSuppression.RoslynRuleId.Should().Be("RULE2");
        secondSuppression.FilePath.Should().Be("PATH2");
        secondSuppression.RoslynIssueLine.Should().Be(111);
        secondSuppression.Hash.Should().BeNull();
        secondSuppression.RoslynLanguage.Should().Be(RoslynLanguage.VB);
    }

    [TestMethod]
    public void Delete_FileIsDeleted()
    {
        testSubject.Delete(SolutionName);

        file.Received(1).Delete(GetFilePath(SolutionName));
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void Delete_DeletionFails_Logs()
    {
        var errorMessage = "deletion failed";
        fileSystem.File.When(x => x.Delete(GetFilePath(SolutionName))).Do(_ => throw new Exception(errorMessage));

        testSubject.Delete(SolutionName);

        file.Received(1).Delete(GetFilePath(SolutionName));
        logger.AssertPartialOutputStrings(string.Format(Strings.RoslynSettingsFileStorageDeleteError, SolutionName, errorMessage));
    }

    [TestMethod]
    public void Delete_CriticalException_ExceptionThrown()
    {
        fileSystem.File.When(x => x.Delete(GetFilePath(SolutionName))).Do(_ => throw new StackOverflowException());

        Action act = () => testSubject.Delete(SolutionName);

        act.Should().Throw<StackOverflowException>();
    }

    private void MockFileSystem(bool fileExists = true)
    {
        file.Exists(Arg.Any<string>()).Returns(fileExists);
        fileSystem.File.Returns(file);

        var directoryObject = Substitute.For<IDirectory>();
        fileSystem.Directory.Returns(directoryObject);
    }

    private void CheckFileWritten(RoslynSettings settings, string solutionName)
    {
        var expectedFilePath = GetFilePath(solutionName);
        var expectedContent = JsonConvert.SerializeObject(settings, Formatting.Indented);

        file.Received(1).WriteAllText(expectedFilePath, expectedContent);
    }

    private static string GetFilePath(string projectKey) => RoslynSettingsFileInfo.GetSettingsFilePath(projectKey);

    private void MockFileReadAllText(RoslynSettings settings) => file.ReadAllText(GetFilePath(settings.SonarProjectKey)).Returns(JsonConvert.SerializeObject(settings));

    private void CreateSaveAndReloadFile(string serializedText)
    {
        // "Save" the data that was written
        file.When(x => x.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
            .Do(callInfo =>
            {
                serializedText = callInfo.ArgAt<string>(1);
            });

        // "Load" the saved data
        // Note: using a function here, so the method returns the value of serializedText when the
        // method is called, rather than when the mock is created (which would always be null)
        file.ReadAllText(Arg.Any<string>())
            .Returns(
                _ =>
                {
                    serializedText.Should().NotBeNull("Test error: data has not been saved");
                    return serializedText;
                });
    }
}
