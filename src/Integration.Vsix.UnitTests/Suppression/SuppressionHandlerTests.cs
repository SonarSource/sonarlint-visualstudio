/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
        private static readonly DiagnosticDescriptor DummyDescriptor = new DiagnosticDescriptor("dummyId",
            "dummy title", "dummy Message", "dummy category", DiagnosticSeverity.Error, false);

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
            op.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("liveIssueFactory");

            op = () => new SuppressionHandler(issueFactoryMock.Object, null);
            op.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serverIssuesProvider");
        }

        [TestMethod]
        public void ShouldReport_LocationNotInSource_ReturnsTrue()
        {
            Diagnostic diag = CreateDiagnostic(CreateNonSourceLocation());

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            bool result = handler.ShouldIssueBeReported(new Mock<SyntaxTree>().Object, diag);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_CannotCreateLiveIssue_ReturnsTrue()
        {
            // Arrange
            Diagnostic diag = CreateDiagnostic(CreateSourceLocation());
            issueFactoryMock.Setup(createMethod)
                .Returns((LiveIssue)null)
                .Verifiable();

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
            Diagnostic diag = CreateDiagnostic(CreateSourceLocation());
            issueFactoryMock.Setup(createMethod)
                .Returns(new LiveIssue(diag, Guid.NewGuid().ToString()))
                .Verifiable();

            issueProviderMock.Setup(getSuppressedIssuesMethod)
                .Returns((IEnumerable<SonarQubeIssue>)null)
                .Verifiable();

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            // Act
            bool result = handler.ShouldIssueBeReported(syntaxTreeMock.Object, diag);

            // Assert
            VerifyLiveIssueCreated(Times.Once());
            VerifyServerIssuesRequested(Times.Once());
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_ProjectIssue_NoMatch_ReturnsTrue()
        {
            Diagnostic diag = CreateDiagnostic(Location.None);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            bool result = handler.ShouldIssueBeReported(new Mock<SyntaxTree>().Object, diag);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_ProjectIssue_MatchExists_ReturnsFalse()
        {
            Diagnostic diag = CreateDiagnostic(Location.None);

            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

            bool result = handler.ShouldIssueBeReported(new Mock<SyntaxTree>().Object, diag);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldReport_NoMatches_ReturnsTrue()
        {
            SuppressionHandler handler = new SuppressionHandler(issueFactoryMock.Object, issueProviderMock.Object);

         //   bool result = handler.ShouldIssueBeReported()

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

        private static Diagnostic CreateDiagnostic(Location loc)
        {
            return Diagnostic.Create(DummyDescriptor, loc);
        }

        private void SetServerIssues(IEnumerable<SonarQubeIssue> issues)
        {
            issueProviderMock.Setup(getSuppressedIssuesMethod).Returns(issues);
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