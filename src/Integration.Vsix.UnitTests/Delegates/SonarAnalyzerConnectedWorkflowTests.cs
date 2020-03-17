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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerConnectedWorkflowTests
    {
        private Mock<IRoslynSuppressionHandler> suppressionHandlerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            suppressionHandlerMock = new Mock<IRoslynSuppressionHandler>();
        }

        #region Ctor Tests
        [TestMethod]
        public void Ctor_WhenVisualStudioWorkspaceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerConnectedWorkflow(null, suppressionHandlerMock.Object);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_WhenISonarQubeServiceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerConnectedWorkflow(new AdhocWorkspace(), null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("suppressionHandler");
        }
        #endregion

        #region VsixAnalyzerReportDiagnostic Tests

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenIssueIsSuppressed_DoesNotCallReportDiagnostic()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, "1");
            var reportingContextMock = CreateReportingContext(diag);

            this.suppressionHandlerMock.Setup(x => x.ShouldIssueBeReported(It.IsAny<SyntaxTree>(), It.IsAny<Diagnostic>()))
                .Returns(false);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            VerifyDiagnostIsNotReported(reportingContextMock);
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenIssueIsNotSuppressed_CallsReportDiagnostic()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, "1");
            var reportingContextMock = CreateReportingContext(diag);

            this.suppressionHandlerMock.Setup(x => x.ShouldIssueBeReported(It.IsAny<SyntaxTree>(), diag))
                .Returns(true);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            VerifyDiagnostIsReported(reportingContextMock);
        }

        #endregion

        private SonarAnalyzerConnectedWorkflow CreateTestSubject() =>
            new SonarAnalyzerConnectedWorkflow(new AdhocWorkspace(), suppressionHandlerMock.Object);

        private Diagnostic CreateFakeDiagnostic(string language, string suffix = "")
        {
            var tags = new List<string> { language };
            return Diagnostic.Create($"id{suffix}", $"category{suffix}", "message", DiagnosticSeverity.Warning,
                DiagnosticSeverity.Warning, true, 1, customTags: tags);
        }

        private static Mock<IReportingContext> CreateReportingContext(Diagnostic diagnosticToReturn)
        {
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diagnosticToReturn);

            return reportingContextMock;
        }

        private static void VerifyDiagnostIsNotReported(Mock<IReportingContext> reportingContextMock) =>
            reportingContextMock.Verify(x => x.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);

        private static void VerifyDiagnostIsReported(Mock<IReportingContext> reportingContextMock) =>
            reportingContextMock.Verify(x => x.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Once);
    }
}

