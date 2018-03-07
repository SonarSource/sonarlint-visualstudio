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
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerStandaloneWorkflowTests
    {
        private Mock<IProjectsRuleSetProvider> ruleSetsProviderMock;

        [TestInitialize]
        public void TestInitialize()
        {
            ruleSetsProviderMock = new Mock<IProjectsRuleSetProvider>();
        }

        [TestMethod]
        public void Ctor_WhenWorkspaceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerStandaloneWorkflow(null, ruleSetsProviderMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_IProjectsRuleSetProviderIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerStandaloneWorkflow(new AdhocWorkspace(), null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("ruleSetsProvider");
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenRuleInSonarWay_CallsReportDiagnostic()
        {
            // Arrange
            var testSubject = new SonarAnalyzerStandaloneWorkflow(new AdhocWorkspace(), ruleSetsProviderMock.Object);
            var diag = CreateFakeDiagnostic(true, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(diag), Times.Once);
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenRuleNotInSonarWay_DoesNotCallReportDiagnostic()
        {
            // Arrange
            var testSubject = new SonarAnalyzerStandaloneWorkflow(new AdhocWorkspace(), ruleSetsProviderMock.Object);
            var diag = CreateFakeDiagnostic(false, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
        }

        private Diagnostic CreateFakeDiagnostic(bool isInSonarWay = false, string suffix = "") =>
            Diagnostic.Create($"id{suffix}", $"category{suffix}", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning,
                true, 1, customTags: isInSonarWay ? new[] { "SonarWay" } : Enumerable.Empty<string>());
    }
}
