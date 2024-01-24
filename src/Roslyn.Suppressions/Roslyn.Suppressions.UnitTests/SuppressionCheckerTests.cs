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
using System.IO;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarQube.Client;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class SuppressionCheckerTests
    {
        // Cache that will throw if it is called
        private static readonly ISettingsCache ThrowingSettingsCache = new Mock<ISettingsCache>(MockBehavior.Strict).Object;

        private readonly Diagnostic ValidInSourceDiagnostic = CreateDiagnostic("any",
            CreateSourceFileLocation("c:\\any.txt"));

        [TestMethod]
        public void IsSuppressed_NonSourceLocation_ReturnsFalse()
        {
            // Perf - should early-out for non-source location
            var testSubject = CreateTestSubject(ThrowingSettingsCache);

            var nonSourceDiagnostic = CreateDiagnostic("any", CreateNonSourceLocation());

            // Act
            var actual = testSubject.IsSuppressed(nonSourceDiagnostic, "any");

            actual.Should().BeFalse();
        }

        [TestMethod]
        public void IsSuppressed_NullDiagnosticLocation_ReturnsFalse()
        {
            // Perf - should early-out for null diagnostic location
            var testSubject = CreateTestSubject(ThrowingSettingsCache);

            var nullLocationDiagnostic = CreateDiagnostic("any", null);

            // Act
            var actual = testSubject.IsSuppressed(nullLocationDiagnostic, "any");

            actual.Should().BeFalse();
        }


        [TestMethod]
        public void IsSuppressed_NullSettings_ReturnsFalse()
        {
            var cache = CreateSettingsCache("settingsKey1", null);
            var testSubject = CreateTestSubject(cache.Object);

            // Act
            var actual = testSubject.IsSuppressed(ValidInSourceDiagnostic, "settingsKey1");

            actual.Should().Be(false);
            CheckGetSettingsCalled(cache, "settingsKey1");
        }

        [TestMethod]
        public void IsSuppressed_EmptySettings_ReturnsFalse()
        {
            var cache = CreateSettingsCache("settingsKey1", new SuppressedIssue[0]);

            var testSubject = CreateTestSubject(cache.Object);

            // Act
            var actual = testSubject.IsSuppressed(ValidInSourceDiagnostic, "settingsKey1");

            actual.Should().Be(false);
            CheckGetSettingsCalled(cache, "settingsKey1");
        }

        [TestMethod]
        public void IsSuppressed_HasIssues_NoMatches_ReturnsFalse()
        {
            var diagnostic = CreateDiagnostic("S111", CreateSourceFileLocation("c:\\myfile.cs"));

            var suppressedIssues = new SuppressedIssue[]
            {
                CreateIssue("S111", "wrongFile1.txt")
            };

            var cache = CreateSettingsCache("settingsKey", suppressedIssues);
            var checksumCalculator = CreateChecksumCalculator("any");
            var testSubject = CreateTestSubject(cache.Object, checksumCalculator.Object);

            // Act
            var actual = testSubject.IsSuppressed(diagnostic, "settingsKey");

            actual.Should().BeFalse();
            CheckGetSettingsCalled(cache, "settingsKey");
        }

        [TestMethod]
        public void IsSuppressed_HasIssues_Matches_ReturnsTrue()
        {
            var location = CreateSourceFileLocation(DiagFileName, DiagFileText, DiagSelectedText);
            var diagnostic = CreateDiagnostic(DiagRuleId, location);

            var suppressedIssues = new[]
            {
                 CreateIssueFromDiagnostic(diagnostic)
            };

            var checksumCalculator = CreateChecksumCalculator(DiagHash);

            var cache = CreateSettingsCache("settingsKey", suppressedIssues);
            var testSubject = CreateTestSubject(cache.Object, checksumCalculator.Object);

            // Act
            var actual = testSubject.IsSuppressed(diagnostic, "settingsKey");

            actual.Should().BeTrue();
            CheckGetSettingsCalled(cache, "settingsKey");
        }

        [TestMethod]
        public void IsMatch_FilesAreDifferent_ReturnsFalse()
        {
            var checksumCalculator = CreateChecksumCalculator("hash");

            var diag = CreateDiagnostic("S999", CreateSourceFileLocation("c:\\diagnosticFileName.cs", "all text", "all"));
            // Sanity check the diagnostic location is on the expected line
            diag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(0, "Test setup error");

            var issue = CreateIssue("S999", "issueFile.cs", 1, "hash");

            SuppressionChecker.IsMatch(diag, issue, checksumCalculator.Object)
                .Should().BeFalse();

            checksumCalculator.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void IsMatch_RuleIdsAreDifferent_ReturnsFalse()
        {
            var checksumCalculator = CreateChecksumCalculator("hash");

            var diag = CreateDiagnostic("S111", CreateSourceFileLocation("c:\\file.cs", "all text", "all"));
            // Sanity check the diagnostic location is on the expected line
            diag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(0, "Test setup error");

            var issue = CreateIssue("S999", "c\\file.cs", 1, "hash");

            SuppressionChecker.IsMatch(diag, issue, checksumCalculator.Object)
                .Should().BeFalse();

            checksumCalculator.Invocations.Count.Should().Be(0);

        }

        #region Matching tests

        // Constants used in for the diagnostic to match against in IsMatch_ReturnsExpectedValue
        // They are defined as constants here so we can use them in the [DataRow]s for the test.
        private const string DiagFileName = "c:\\diag.txt";
        private const string MatchingServerFileName = "diag.txt";
        private const string DiagRuleId = "S999";
        private const string DiagHash = "diag hash";

        private const string DiagFileText = "0\n1\n2\n3 text \n\n";
        private const string DiagSelectedText = "text";
        private const int DiagRoslynLineNumber = 3; // the 0-base line in which the word "text" appears

        [TestMethod]
        [DataRow(MatchingServerFileName, DiagRuleId, DiagRoslynLineNumber, DiagHash, true)]
        [DataRow("wrong file name.cs", DiagRuleId, DiagRoslynLineNumber, DiagHash, false)]
        [DataRow(MatchingServerFileName, "wrong rule id", DiagRoslynLineNumber, DiagHash, false)]
        [DataRow(MatchingServerFileName, DiagRuleId, 999, "wrong hash", false)] // wrong line, wrong hash -> false
        [DataRow(MatchingServerFileName, DiagRuleId, DiagRoslynLineNumber, "wrong hash", true)] // right line, wrong hash -> true
        [DataRow(MatchingServerFileName, DiagRuleId, 888, DiagHash, true)] // wrong line, right hash -> true

        // Special cases
        [DataRow("DIAG.TXT", DiagRuleId, DiagRoslynLineNumber, DiagHash, true)] // case-insensitive
        [DataRow(MatchingServerFileName, "s999", DiagRoslynLineNumber, DiagHash, true)] // case-insensitive
        public void IsMatch_ReturnsExpectedValue(string issueFile, string issueRuleId, int issueLine,
            string issueHash, bool expected)
        {
            // The diagnostic is the same in every case
            var checksumCalculator = CreateChecksumCalculator(DiagHash);
            var diag = CreateDiagnostic(DiagRuleId, CreateSourceFileLocation(DiagFileName, DiagFileText, DiagSelectedText));
            // Sanity check the diagnostic location is on the expected line
            diag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(DiagRoslynLineNumber, "Test setup error");

            var issue = CreateIssue(issueRuleId, issueFile, issueLine, issueHash);

            SuppressionChecker.IsMatch(diag, issue, checksumCalculator.Object)
                .Should().Be(expected);
        }

        [TestMethod]
        [DataRow(0, 0, true)]
        [DataRow(1, 0, false)]
        [DataRow(1, 1, false)]
        [DataRow(2, 0, false)]
        [DataRow(2, 2, false)]
        public void IsMatch_IsRoslynFileLevelIssue_MatchesSonarFileLevelIssue(int roslynStartPosition, int length,
            bool expected)
        {
            const string text = "000\n111\n222\n";
            var selectedSpan = new TextSpan(roslynStartPosition, length);
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(text, path: MatchingWellKnownLocalPath);
            var location = Location.Create(syntaxTree, selectedSpan);
            var diagnostic = CreateDiagnostic("id", location);

            var sonarFileLevelIssue = CreateIssue(ruleId: "id", path: WellKnownRelativeServerPath, hash: "hash", line: null);

            var actual = SuppressionChecker.IsMatch(diagnostic, sonarFileLevelIssue, Mock.Of<IChecksumCalculator>());

            actual.Should().Be(expected);
        }

        [TestMethod]
        public void IsMatch_RoslynFileLevelIssue_SonarNonFileLevelIssue_DoNotMatch()
        {
            const string text = "000\n111\n222\n";
            var selectedSpan = new TextSpan(0, 0);
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(text, path: MatchingWellKnownLocalPath);
            var location = Location.Create(syntaxTree, selectedSpan);
            var diagnostic = CreateDiagnostic("id", location);

            var sonarNonFileLevelIssue = CreateIssue(ruleId: "id", path: WellKnownRelativeServerPath, hash: "hash", line: 2);

            var actual = SuppressionChecker.IsMatch(diagnostic, sonarNonFileLevelIssue, Mock.Of<IChecksumCalculator>());

            actual.Should().BeFalse();
        }

        private const string WellKnownRelativeServerPath = "file.txt";
        private const string MatchingWellKnownLocalPath = "c:\\" + WellKnownRelativeServerPath;

        [TestMethod]
        [DataRow(@"same.txt", @"c:\same.txt", true)]
        [DataRow(@"SAME.TXT", @"c:\same.txt", true)]
        [DataRow(@"differentExt.123", @"d:\differentExt.999", false)]
        [DataRow(@"partial\file.cs", @"c:\aaa\partial\file.cs", true)]
        [DataRow(@"aaa\partial\file.cs", @"x:\partial\file.cs", false)]
        public void IsMatch_IsSameFile(string serverIssueFile, string diagFile, bool expected)
        {
            var checksumCalculator = CreateChecksumCalculator("a hash that won't match");

            var diag = CreateDiagnostic(DiagRuleId, CreateSourceFileLocation(diagFile, DiagFileText, DiagSelectedText));

            var issue = CreateIssueFromDiagnostic(diag, overrideFilePath: serverIssueFile);

            SuppressionChecker.IsMatch(diag, issue, checksumCalculator.Object)
                .Should().Be(expected);
        }

        #endregion // Matching tests

        private void CheckGetSettingsCalled(Mock<ISettingsCache> cache, string expectedSettingsKey) =>
            cache.Verify(x => x.GetSettings(expectedSettingsKey), Times.Once());

        private static SuppressionChecker CreateTestSubject(ISettingsCache cache = null,
            IChecksumCalculator checksumCalculator = null)
        {
            cache ??= CreateSettingsCache("any", null).Object;
            checksumCalculator ??= CreateChecksumCalculator("any").Object;

            return new SuppressionChecker(cache, checksumCalculator);
        }

        private static Mock<ISettingsCache> CreateSettingsCache(string settingsKey, IEnumerable<SuppressedIssue> suppressedIssues)
        {
            var cache = new Mock<ISettingsCache>();
            var settings = new RoslynSettings { Suppressions = suppressedIssues };
            cache.Setup(x => x.GetSettings(settingsKey)).Returns(settings);
            return cache;
        }

        private static Mock<IChecksumCalculator> CreateChecksumCalculator(string hashToReturn)
        {
            var calculator = new Mock<IChecksumCalculator>();
            calculator.Setup(x => x.Calculate(It.IsAny<string>()))
                .Returns(hashToReturn);

            return calculator;
        }

        /// <summary>
        /// Returns an issue with the same values as the supplied diagnostic.
        /// </summary>
        /// <remarks>
        /// The issue will match the diagnostic, unless the file path is overridden.
        /// The diagnostic must have a valid location and source tree.
        /// </remarks>
        private static SuppressedIssue CreateIssueFromDiagnostic(Diagnostic diagnostic,
            string overrideFilePath = null)
        {
            // The issue will match because the line number is the same (so the hash is irrelevant)
            var sonarLine = diagnostic.Location.GetLineSpan().EndLinePosition.Line;

            var serverIssuePath = overrideFilePath ?? GetMatchingRelativePath(diagnostic.Location.SourceTree.FilePath);

            var issueHash = Guid.NewGuid().ToString();

            return CreateIssue(diagnostic.Id, serverIssuePath, sonarLine, issueHash);
        }

        private static string GetMatchingRelativePath(string absolutePath)
        {
            // The matching server should not be rooted, so we'll strip off the
            // root and return the rest of the path. The product code should be
            // considered that to be a valid match for the original full path.
            if (!Path.IsPathRooted(absolutePath))
            {
                throw new ArgumentException(nameof(absolutePath), $"Test setup error: path should be rooted. Actual: {absolutePath}");
            }
            var root = Path.GetPathRoot(absolutePath);
            return absolutePath.Substring(root.Length);
        }

        #region Diagnostic helper methods

        private static Location CreateNonSourceLocation()
        {
            var nonSourceLocation = Location.Create("dummyFilePath.cs", new TextSpan(), new LinePositionSpan());
            nonSourceLocation.IsInSource.Should().BeFalse();
            nonSourceLocation.Kind.Should().NotBe(LocationKind.SourceFile);
            return nonSourceLocation;
        }

        /// <summary>
        /// Returns a Location that is in a source file, backed by a valid syntax tree.
        /// </summary>
        /// <param name="fileText">The contents of the entire file (optional)</param>
        /// <param name="selectedText">The specific text in the file the location points to (optional)</param>
        private static Location CreateSourceFileLocation(string filePath,
            string fileText = "any", string selectedText = "any")
        {
            // Mocking source location so that it appears to be backed by a real syntax tree, with
            // the required "GetLineSpan()", "GetText().Lines" etc methods working correctly is hard.

            // The simplest thing to do is create a real C# syntax tree. It doesn't matter if the
            // contents of the fake file are not valid C# - the parser will still produce a valid
            // document.

            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(fileText, path: filePath);
            var selectedSpan = CreateSpan(fileText, selectedText);
            var location = Location.Create(syntaxTree, selectedSpan);

            // Sanity checks that the objects are set up as we expected
            location.IsInSource.Should().BeTrue("Test setup error: location should be in source");
            location.Kind.Should().Be(LocationKind.SourceFile, "Test setup error: LocationKind should be SourceFile");
            syntaxTree.FilePath.Should().Be(filePath, "Test setup error: syntaxTree FilePath is not set correctly");
            var lineSpan = location.GetLineSpan();
            lineSpan.IsValid.Should().BeTrue("Test setup error: lineSpan is not valid");
            var lineText = syntaxTree.GetText().Lines[lineSpan.EndLinePosition.Line].ToString();
            lineText.Should().Contain(selectedText, "Test setup error: the fake location/syntax tree is not correctly constructed");

            return location;
        }

        private static TextSpan CreateSpan(string documentText, string selectedText)
        {
            var start = documentText.IndexOf(selectedText);
            start.Should().BeGreaterThan(-1, "Test setup error: selected text is not in the document text");
            return new TextSpan(start, selectedText.Length);
        }

        private static Diagnostic CreateDiagnostic(string ruleId, Location loc)
        {
            var diagnostic = new Mock<Diagnostic>();

            diagnostic.Setup(x => x.Id).Returns(ruleId);
            diagnostic.Setup(x => x.Location).Returns(loc);

            return diagnostic.Object;
        }

        #endregion
    }
}
