/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class SuppressionHandlerTests
    {
        private readonly Expression<Func<ILiveIssueFactory, LiveIssue>> createMethod =
            x => x.Create(It.IsAny<SyntaxTree>(), It.IsAny<Diagnostic>());

        private readonly Expression<Func<ISonarQubeIssuesProvider, IEnumerable<SonarQubeIssue>>> getSuppressedIssuesMethod =
            x => x.GetSuppressedIssues(It.IsAny<string>(), It.IsAny<string>());

        private Mock<ILiveIssueFactory> issueFactoryMock;
        private Mock<ISonarQubeIssuesProvider> issueProviderMock;
        private Mock<SyntaxTree> syntaxTreeMock;

        [TestInitialize]
        public void TestInitialize()
        {
            issueFactoryMock = new Mock<ILiveIssueFactory>();
            issueProviderMock = new Mock<ISonarQubeIssuesProvider>();
            syntaxTreeMock = new Mock<SyntaxTree>();
        }

        [TestMethod]
        public void Ctor_Arguments()
        {
            Action op = () => new SuppressionHandler(null, issueProviderMock.Object);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("liveIssueFactory");

            op = () => new SuppressionHandler(issueFactoryMock.Object, null);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serverIssuesProvider");
        }

        [TestMethod]
        public void ShouldReport_LocationNotInSource_ReturnsTrue()
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic("dummy rule ID", CreateNonSourceLocation());

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(new Mock<SyntaxTree>().Object, diag);

            // Assert
            result.Should().BeTrue();

            // Should early-out
            VerifyLiveIssueCreated(Times.Never());
            VerifyServerIssuesRequested(Times.Never());
        }

        [TestMethod]
        public void ShouldReport_CannotCreateLiveIssue_ReturnsTrue()
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic("dummy rule id", CreateSourceLocation());
            SetLiveIssue(null);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            VerifyLiveIssueCreated(Times.Once());
            VerifyServerIssuesRequested(Times.Never());
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_NoServerIssues_ReturnsTrue()
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic("dummy rule id", CreateSourceLocation());
            SetLiveIssue(diag, startLine: 1, wholeLineText: "text");
            SetServerIssues(null);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            VerifyLiveIssueCreated(Times.Once());
            VerifyServerIssuesRequested(Times.Once());
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_NoMatchingServerIssues_ReturnsTrue()
        {
            // Arrange
            string wholeLineText = "whole line text";
            string lineHash = ChecksumCalculator.Calculate(wholeLineText);

            Diagnostic diag = CreateDiagnostic("RuleId 1", CreateSourceLocation());
            SetLiveIssue(diag, startLine: 10, wholeLineText: wholeLineText);

            var serverIssue1 = CreateServerIssue("Wrong rule id", 10, lineHash); // wrong rule id
            var serverIssue2 = CreateServerIssue("RuleId 1", 999, "wrong hash"); // wrong line and hash
            var serverIssue3 = CreateServerIssue("RuleId 1", 999, lineHash.ToUpperInvariant()); // wrong line and wrong-case hash

            SetServerIssues(serverIssue1, serverIssue2, serverIssue3);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_ServerIssueMatchesOnLine_ReturnsFalse()
        {
            // Arrange
            string wholeLineText = "whole line text";
            string lineHash = ChecksumCalculator.Calculate(wholeLineText);

            Diagnostic diag = CreateDiagnostic("RightRuleId", CreateSourceLocation());
            SetLiveIssue(diag, startLine: 101, wholeLineText: wholeLineText);

            var serverIssue = CreateServerIssue("RightRuleId", 101, "wrong hash");
            SetServerIssues(serverIssue);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldReport_ServerIssueMatchesOnLineHash_ReturnsFalse()
        {
            // Arrange
            string wholeLineText = "whole line text";
            string lineHash = ChecksumCalculator.Calculate(wholeLineText);

            Diagnostic diag = CreateDiagnostic("RightRuleId", CreateSourceLocation());
            SetLiveIssue(diag, startLine: 101, wholeLineText: wholeLineText);

            var serverIssue = CreateServerIssue("RIGHTRULEID", 999, lineHash); // rule id comparison is case-insensitive
            SetServerIssues(serverIssue);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldReport_FileOrProjectIssue_NoMatch_ReturnsTrue()
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic("RightRuleId", CreateSourceLocation());
            SetLiveIssue(diag, startLine: 0, wholeLineText: "");

            var serverIssue1 = CreateServerIssue("WrongRuleId", 0, "");
            var serverIssue2 = CreateServerIssue("RightRuleId", 1, "wrong hash"); // wrong line and hash
            var serverIssue3 = CreateServerIssue("RightRuleId", 999, "wrong hash"); // not a file/project issue -> no match
            SetServerIssues(serverIssue1, serverIssue2, serverIssue3);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(new Mock<SyntaxTree>().Object, diag);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_FileOrProjectIssue_MatchExists_ReturnsFalse()
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic("RightRuleId", CreateSourceLocation());
            SetLiveIssue(diag, startLine: 0, wholeLineText: "");

            var serverIssue1 = CreateServerIssue("WrongRuleId", 0, "");
            var serverIssue2 = CreateServerIssue("RightRuleId", 0, "wrong hash"); // wrong hash
            var serverIssue3 = CreateServerIssue("RightRuleId", 0, "");

            SetServerIssues(serverIssue1, serverIssue2, serverIssue3);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(new Mock<SyntaxTree>().Object, diag);

            // Assert
            result.Should().BeFalse();
        }

        private static Location CreateNonSourceLocation()
        {
            var nonSourceLocation = Location.Create("dummyFilePath.cs", new TextSpan(), new LinePositionSpan());
            nonSourceLocation.IsInSource.Should().BeFalse();
            return nonSourceLocation;
        }

        private Location CreateSourceLocation()
        {
            var sourceLocation = Location.Create(syntaxTreeMock.Object, new TextSpan());
            sourceLocation.IsInSource.Should().BeTrue();
            return sourceLocation;
        }

        private static Diagnostic CreateDiagnostic(string ruleId, Location loc)
        {
            DiagnosticDescriptor descriptor = new DiagnosticDescriptor(ruleId,
                "dummy title", "dummy Message", "dummy category", DiagnosticSeverity.Error, false);

            return Diagnostic.Create(descriptor, loc);
        }

        private static LiveIssue CreateLiveIssue()
        {
            return null;
        }

        private static SonarQubeIssue CreateServerIssue(string ruleId, int line, string lineHash)
        {
            // The IServerIssuesProvider is responsible for matching on the file path and module key,
            // so it doesn't matter what values we set here
            return new SonarQubeIssue(
                filePath: "irrelevant path.cs",
                hash: lineHash,
                line: line,
                message: "irrelevant message",
                moduleKey: "irrelevant module key",
                resolutionState: SonarQubeIssueResolutionState.FalsePositive,
                ruleId: ruleId
                );
        }

        private void SetLiveIssue(Diagnostic diagnostic, int startLine, string wholeLineText)
        {
            LiveIssue liveIssue = new LiveIssue(diagnostic, Guid.NewGuid().ToString(),
                issueFilePath: "dummy file path",
                startLine: startLine,
                wholeLineText: wholeLineText);

            SetLiveIssue(liveIssue);
        }

        private void SetLiveIssue(LiveIssue liveIssue)
        {
            issueFactoryMock.Setup(createMethod)
                .Returns(liveIssue)
                .Verifiable();
        }

        private void SetServerIssues(params SonarQubeIssue[] issues)
        {
            issueProviderMock.Setup(getSuppressedIssuesMethod)
                .Returns(issues)
                .Verifiable();
        }

        private void VerifyLiveIssueCreated(Times expected)
        {
            issueFactoryMock.Verify(createMethod, expected);
        }

        private void VerifyServerIssuesRequested(Times expected)
        {
            issueProviderMock.Verify(getSuppressedIssuesMethod, expected);
        }
    }
}