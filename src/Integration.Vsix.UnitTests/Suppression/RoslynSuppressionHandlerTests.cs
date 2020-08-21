/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class RoslynSuppressionHandlerTests
    {
        private readonly Expression<Func<IRoslynLiveIssueFactory, LiveIssue>> createMethod =
            x => x.Create(It.IsAny<SyntaxTree>(), It.IsAny<Diagnostic>());

        private readonly Expression<Func<ISuppressedIssueMatcher, bool>> suppressionExistsMethod =
            x => x.SuppressionExists(It.IsAny<IFilterableIssue>());

        private Mock<IRoslynLiveIssueFactory> issueFactoryMock;
        private Mock<ISuppressedIssueMatcher> issueMatcherMock;
        private Mock<SyntaxTree> syntaxTreeMock;

        [TestInitialize]
        public void TestInitialize()
        {
            issueFactoryMock = new Mock<IRoslynLiveIssueFactory>();
            issueMatcherMock = new Mock<ISuppressedIssueMatcher>();
            syntaxTreeMock = new Mock<SyntaxTree>();
        }

        [TestMethod]
        public void Ctor_Arguments()
        {
            Action op = () => new RoslynSuppressionHandler(null, issueMatcherMock.Object);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("liveIssueFactory");

            op = () => new RoslynSuppressionHandler(issueFactoryMock.Object, null);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issueMatcher");
        }

        [TestMethod]
        public void ShouldReport_LocationNotInSource_ReturnsTrue()
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic("dummy rule ID", CreateNonSourceLocation());

            RoslynSuppressionHandler handler = new RoslynSuppressionHandler(issueFactoryMock.Object, issueMatcherMock.Object);

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

            RoslynSuppressionHandler handler = new RoslynSuppressionHandler(issueFactoryMock.Object, issueMatcherMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            VerifyLiveIssueCreated(Times.Once());
            VerifyServerIssuesRequested(Times.Never());
            result.Should().BeTrue();
        }

        [DataTestMethod]
        [DataRow(false, true)]
        [DataRow(true, false)]
        public void ShouldReport_MatchingSuppressionExists(bool suppressionExists, bool expectedShouldIssueBeReported)
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic("dummy rule id", CreateSourceLocation());
            SetLiveIssue(diag, startLine: 1, wholeLineText: "text");
            SetSuppressionExistsResponse(suppressionExists);

            RoslynSuppressionHandler handler = new RoslynSuppressionHandler(issueFactoryMock.Object, issueMatcherMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            VerifyLiveIssueCreated(Times.Once());
            VerifyServerIssuesRequested(Times.Once());
            result.Should().Be(expectedShouldIssueBeReported);
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

        private void SetLiveIssue(Diagnostic diagnostic, int startLine, string wholeLineText)
        {
            LiveIssue liveIssue = new LiveIssue(diagnostic.Id, Guid.NewGuid().ToString(),
                filePath: "dummy file path",
                startLine: startLine,
                lineHash: ChecksumCalculator.Calculate(wholeLineText));

            SetLiveIssue(liveIssue);
        }

        private void SetLiveIssue(LiveIssue liveIssue)
        {
            issueFactoryMock.Setup(createMethod)
                .Returns(liveIssue)
                .Verifiable();
        }

        private void SetSuppressionExistsResponse(bool suppressionExists)
        {
            issueMatcherMock.Setup(suppressionExistsMethod)
                .Returns(suppressionExists)
                .Verifiable();
        }

        private void VerifyLiveIssueCreated(Times expected)
        {
            issueFactoryMock.Verify(createMethod, expected);
        }

        private void VerifyServerIssuesRequested(Times expected)
        {
            issueMatcherMock.Verify(suppressionExistsMethod, expected);
        }
    }
}
