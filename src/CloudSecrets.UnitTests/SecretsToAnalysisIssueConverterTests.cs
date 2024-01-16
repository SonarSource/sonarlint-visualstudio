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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.Secrets.DotNet;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;

namespace SonarLint.VisualStudio.CloudSecrets.UnitTests
{
    [TestClass]
    public class SecretsToAnalysisIssueConverterTests
    {
        private const string ValidRuleKey = "some rule key";
        private const string ValidRuleMessage = "some rule message";
        private const string ValidFilePath = "some file path";

        [TestMethod]
        public void Convert_FieldsPopulated()
        {
            var secretsDetector = CreateSecretsDetector(ValidRuleKey, ValidRuleMessage);
            var secret = CreateSecret(2, 1);

            var textSnapshot = CreateTextSnapshot();
            var dummyLine = CreateVsLine(textSnapshot.Object, 0, 1, 2);
            textSnapshot.Setup(x => x.GetLineFromPosition(It.IsAny<int>())).Returns(dummyLine);

            var testSubject = CreateTestSubject();
            var convertedIssue = testSubject.Convert(secret, secretsDetector, ValidFilePath, textSnapshot.Object);

            convertedIssue.RuleKey.Should().Be(ValidRuleKey);
            convertedIssue.Severity.Should().Be(AnalysisIssueSeverity.Major);
            convertedIssue.Type.Should().Be(AnalysisIssueType.Vulnerability);

            convertedIssue.PrimaryLocation.FilePath.Should().Be(ValidFilePath);
            convertedIssue.PrimaryLocation.Message.Should().Be(ValidRuleMessage);
            convertedIssue.PrimaryLocation.TextRange.LineHash.Should().BeNull();

            convertedIssue.Flows.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow("", "")] // empty should not throw
        [DataRow("a.txt", "a.txt")] // not-rooted should stay the same
        [DataRow("c:\\a.txt", "c:\\a.txt")]
        [DataRow("c:/a.txt", "c:\\a.txt")]
        [DataRow("c:/a/b/c.txt", "c:\\a\\b\\c.txt")]
        [DataRow("c:/a\\b/c.txt", "c:\\a\\b\\c.txt")]
        public void Convert_FilePath_QualifiedFilePath(string originalPath, string expectedPath)
        {
            var secretsDetector = CreateSecretsDetector(ValidRuleKey, ValidRuleMessage);
            var secret = CreateSecret(2, 1);

            var textSnapshot = CreateTextSnapshot();
            var dummyLine = CreateVsLine(textSnapshot.Object, 0, 1, 2);
            textSnapshot.Setup(x => x.GetLineFromPosition(It.IsAny<int>())).Returns(dummyLine);

            var testSubject = CreateTestSubject();
            var convertedIssue = testSubject.Convert(secret, secretsDetector, originalPath, textSnapshot.Object);

            convertedIssue.PrimaryLocation.FilePath.Should().Be(expectedPath);
        }

        [TestMethod]
        public void Convert_SingleLineSecret_PositionConverted()
        {
            var secretsDetector = CreateSecretsDetector(ValidRuleKey, ValidRuleMessage);
            var secret = CreateSecret(200, 100);

            var textSnapshot = CreateTextSnapshot();
            var vsLine = CreateVsLine(textSnapshot.Object, 20, 150, 300);

            SetupTextSnapshot(textSnapshot, secret, vsLine, vsLine);

            var lineHashCalculator = new Mock<ILineHashCalculator>();
            lineHashCalculator.Setup(x => x.Calculate(textSnapshot.Object, 21)).Returns("IamAHash");

            var testSubject = CreateTestSubject(lineHashCalculator.Object);
            var convertedIssue = testSubject.Convert(secret, secretsDetector, ValidFilePath, textSnapshot.Object);

            convertedIssue.PrimaryLocation.TextRange.StartLine.Should().Be(21);
            convertedIssue.PrimaryLocation.TextRange.EndLine.Should().Be(21);

            convertedIssue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(50); // secret.StartIndex - vsLine.Start.Position
            convertedIssue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(150); // secret.EndIndex - vsLine.End.Position

            convertedIssue.PrimaryLocation.TextRange.LineHash.Should().Be("IamAHash");
        }

        [TestMethod]
        public void Convert_MultiLineSecret_PositionConverted()
        {
            var secretsDetector = CreateSecretsDetector(ValidRuleKey, ValidRuleMessage);
            var secret = CreateSecret(200, 100);

            var textSnapshot = CreateTextSnapshot();
            var vsStartLine = CreateVsLine(textSnapshot.Object, 20, 150, 100);
            var vsEndLine = CreateVsLine(textSnapshot.Object, 21, 250, 100);

            SetupTextSnapshot(textSnapshot, secret, vsStartLine, vsEndLine);

            var lineHashCalculator = new Mock<ILineHashCalculator>();
            lineHashCalculator.Setup(x => x.Calculate(textSnapshot.Object, 21)).Returns("IamAHash");

            var testSubject = CreateTestSubject(lineHashCalculator.Object);
            var convertedIssue = testSubject.Convert(secret, secretsDetector, ValidFilePath, textSnapshot.Object);

            convertedIssue.PrimaryLocation.TextRange.StartLine.Should().Be(21);
            convertedIssue.PrimaryLocation.TextRange.EndLine.Should().Be(22);

            convertedIssue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(50); // secret.StartIndex - vsLine.Start.Position
            convertedIssue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(50); // secret.EndIndex - vsLine.End.Position

            convertedIssue.PrimaryLocation.TextRange.LineHash.Should().Be("IamAHash");
        }

        private ISecretsToAnalysisIssueConverter CreateTestSubject(ILineHashCalculator lineHashCalculator = null)
        {
            lineHashCalculator ??= Mock.Of<ILineHashCalculator>();

            var testSubject = new SecretsToAnalysisIssueConverter(lineHashCalculator);

            return testSubject;
        }

        private ISecretDetector CreateSecretsDetector(string ruleKey, string message)
        {
            var secretDetector = new Mock<ISecretDetector>();
            secretDetector.Setup(x => x.RuleKey).Returns(ruleKey);
            secretDetector.Setup(x => x.Message).Returns(message);

            return secretDetector.Object;
        }

        private ISecret CreateSecret(int startIndex, int length)
        {
            var secret = new Mock<ISecret>();
            secret.Setup(x => x.StartIndex).Returns(startIndex);
            secret.Setup(x => x.Length).Returns(length);

            return secret.Object;
        }

        private ITextSnapshotLine CreateVsLine(ITextSnapshot textSnapshot, int zeroBasedLineNumber, int lineStartPosition, int lineLength)
        {
            var startLineMock = new Mock<ITextSnapshotLine>();

            startLineMock.SetupGet(x => x.LineNumber)
                .Returns(zeroBasedLineNumber);

            startLineMock.SetupGet(x => x.Start)
                .Returns(() => new SnapshotPoint(textSnapshot, lineStartPosition));

            startLineMock.SetupGet(x => x.End)
                .Returns(() => new SnapshotPoint(textSnapshot, lineLength - lineStartPosition));

            startLineMock.SetupGet(x => x.Length)
                .Returns(() => new SnapshotPoint(textSnapshot, lineLength));

            return startLineMock.Object;
        }

        private Mock<ITextSnapshot> CreateTextSnapshot()
        {
            var textSnapshot = new Mock<ITextSnapshot>();
            textSnapshot.SetupGet(x => x.Length).Returns(10000000);

            return textSnapshot;
        }

        private void SetupTextSnapshot(Mock<ITextSnapshot> snapshot,
            ISecret secret,
            ITextSnapshotLine vsStartLine = null,
            ITextSnapshotLine vsEndLine = null)
        {
            snapshot.Setup(x => x.GetLineFromPosition(secret.StartIndex)).Returns(vsStartLine);
            snapshot.Setup(x => x.GetLineFromPosition(secret.StartIndex + secret.Length)).Returns(vsEndLine);
        }
    }
}
