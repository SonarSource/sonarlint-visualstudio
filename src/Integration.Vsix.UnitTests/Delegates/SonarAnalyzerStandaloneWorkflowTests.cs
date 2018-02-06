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
using static SonarLint.VisualStudio.Integration.Vsix.SonarAnalyzerWorkflowBase;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerStandaloneWorkflowTests
    {
        private class TestableSonarAnalyzerStandaloneWorkflow : SonarAnalyzerStandaloneWorkflow
        {
            public TestableSonarAnalyzerStandaloneWorkflow()
                : base(new AdhocWorkspace())
            {
            }

            public Func<SyntaxTree, ProjectAnalyzerStatus> ProjectNuGetAnalyzerStatusFunc { get; set; } =
                tree => ProjectAnalyzerStatus.NoAnalyzer;

            protected override ProjectAnalyzerStatus GetProjectNuGetAnalyzerStatus(SyntaxTree syntaxTree) =>
                ProjectNuGetAnalyzerStatusFunc(syntaxTree);
        }

        [TestMethod]
        public void Ctor_WhenWorkspaceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerStandaloneWorkflow(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_ChangeExpectedDelegates()
        {
            // Arrange
            Func<IAnalysisRunContext, bool> expectedShouldExecuteRuleFunc = ctx => false;
            SonarAnalysisContext.ShouldExecuteRuleFunc = expectedShouldExecuteRuleFunc;
            Action<IReportingContext> expectedReportDiagnosticAction = ctx => { };
            SonarAnalysisContext.ReportDiagnosticAction = expectedReportDiagnosticAction;

            // Act
            var testSubject = new TestableSonarAnalyzerStandaloneWorkflow();

            // Assert
            SonarAnalysisContext.ShouldExecuteRuleFunc.Should().NotBe(expectedShouldExecuteRuleFunc);
            SonarAnalysisContext.ReportDiagnosticAction.Should().NotBe(expectedReportDiagnosticAction);
        }

        [TestMethod]
        public void ShouldExecuteVsixAnalyzer_WhenSyntaxTreeIsNull_ReturnsFalse()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerStandaloneWorkflow();
            var analysisRunContextMock = new Mock<IAnalysisRunContext>();

            // Act
            var result = testSubject.ShouldExecuteVsixAnalyzer(analysisRunContextMock.Object);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExecuteVsixAnalyzer_WhenProjectHasNuGetAnalyzer_ReturnsFalse()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerStandaloneWorkflow();
            var analysisRunContextMock = new Mock<IAnalysisRunContext>();

            // Act 1
            testSubject.ProjectNuGetAnalyzerStatusFunc = tree => ProjectAnalyzerStatus.DifferentVersion;
            var result1 = testSubject.ShouldExecuteVsixAnalyzer(analysisRunContextMock.Object);

            // Act 2
            testSubject.ProjectNuGetAnalyzerStatusFunc = tree => ProjectAnalyzerStatus.SameVersion;
            var result2 = testSubject.ShouldExecuteVsixAnalyzer(analysisRunContextMock.Object);

            // Assert
            result1.Should().BeFalse();
            result2.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExecuteVsixAnalyzer_WhenAnyRuleInSonarWay_ReturnsTrue()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerStandaloneWorkflow();
            var diag1 = CreateFakeDiagnostic(false, "1");
            var diag2 = CreateFakeDiagnostic(true, "2");
            var descriptors = new[] { diag1, diag2 }.Select(x => x.Descriptor);
            var analysisRunContextMock = new Mock<IAnalysisRunContext>();
            analysisRunContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            analysisRunContextMock.SetupGet(x => x.SupportedDiagnostics).Returns(descriptors);

            // Act
            var result = testSubject.ShouldExecuteVsixAnalyzer(analysisRunContextMock.Object);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldExecuteVsixAnalyzer_WhenNoRuleInSonarWay_ReturnsFalse()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerStandaloneWorkflow();
            var diag1 = CreateFakeDiagnostic(false, "1");
            var diag2 = CreateFakeDiagnostic(false, "2");
            var descriptors = new[] { diag1, diag2 }.Select(x => x.Descriptor);
            var analysisRunContextMock = new Mock<IAnalysisRunContext>();
            analysisRunContextMock.SetupGet(x => x.SupportedDiagnostics).Returns(descriptors);

            // Act
            var result = testSubject.ShouldExecuteVsixAnalyzer(analysisRunContextMock.Object);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenRuleInSonarWay_CallsReportDiagnostic()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerStandaloneWorkflow();
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
            var testSubject = new TestableSonarAnalyzerStandaloneWorkflow();
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
