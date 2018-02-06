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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Rules;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;
using static SonarLint.VisualStudio.Integration.Vsix.SonarAnalyzerWorkflowBase;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerConnectedWorkflowTests
    {
        private class TestableSonarAnalyzerConnectedWorkflow : SonarAnalyzerConnectedWorkflow
        {
            public TestableSonarAnalyzerConnectedWorkflow(IQualityProfileProvider qualityProfileProvider,
                BoundSonarQubeProject boundProject, ISuppressionHandler suppressionHandler)
                : base(new AdhocWorkspace(), qualityProfileProvider, boundProject, suppressionHandler)
            {
            }

            public Func<SyntaxTree, ProjectAnalyzerStatus> ProjectNuGetAnalyzerStatusFunc { get; set; } =
                tree => ProjectAnalyzerStatus.NoAnalyzer;

            protected override ProjectAnalyzerStatus GetProjectNuGetAnalyzerStatus(SyntaxTree syntaxTree) =>
                ProjectNuGetAnalyzerStatusFunc(syntaxTree);

            public Func<SyntaxTree, Language> LanguageFunc { get; set; }

            internal override Language GetLanguage(SyntaxTree syntaxTree) =>
                LanguageFunc != null ? LanguageFunc(syntaxTree) : base.GetLanguage(syntaxTree);
        }

        private Mock<IQualityProfileProvider> qualityProfileProviderMock;
        private Mock<ISuppressionHandler> suppressionHandlerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            qualityProfileProviderMock = new Mock<IQualityProfileProvider>();
            suppressionHandlerMock = new Mock<ISuppressionHandler>();
        }

        #region Ctor Tests
        [TestMethod]
        public void Ctor_WhenVisualStudioWorkspaceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerConnectedWorkflow(null, qualityProfileProviderMock.Object,
                new BoundSonarQubeProject(), suppressionHandlerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_WhenIQualityProfileProviderIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerConnectedWorkflow(new AdhocWorkspace(), null, new BoundSonarQubeProject(),
                suppressionHandlerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("qualityProfileProvider");
        }

        [TestMethod]
        public void Ctor_WhenBoundSonarQubeProjectIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerConnectedWorkflow(new AdhocWorkspace(), qualityProfileProviderMock.Object, null,
                suppressionHandlerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("boundProject");
        }

        [TestMethod]
        public void Ctor_WhenISonarQubeServiceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerConnectedWorkflow(new AdhocWorkspace(), qualityProfileProviderMock.Object,
                new BoundSonarQubeProject(), null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("suppressionHandler");
        }
        #endregion

        #region GetLanguage Tests
        [TestMethod]
        public void GetLanguage_WhenCSharpSyntaxTree_ReturnsCSharp()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var syntaxTree = CSharpSyntaxTree.ParseText("public class Foo {}");

            // Act
            var result = testSubject.GetLanguage(syntaxTree);

            // Assert
            result.Should().Be(Language.CSharp);
        }

        [TestMethod]
        public void GetLanguage_WhenVisualBasicSyntaxTree_ReturnsVBNet()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var syntaxTree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText(@"Module Program
End Module");

            // Act
            var result = testSubject.GetLanguage(syntaxTree);

            // Assert
            result.Should().Be(Language.VBNET);
        }

        [TestMethod]
        public void GetLanguage_WhenSyntaxTreeIsNull_ReturnsUnknown()
        {
            // Arrange
            var testSubject = CreateTestSubject();

            // Act
            var result = testSubject.GetLanguage(null);

            // Assert
            result.Should().Be(Language.Unknown);
        }

        [TestMethod]
        public void GetLanguage_WhenSyntaxTreeRootIsNull_ReturnsUnknown()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var syntaxTree = new Mock<SyntaxTree>();

            // Act
            var result = testSubject.GetLanguage(syntaxTree.Object);

            // Assert
            result.Should().Be(Language.Unknown);
        }
        #endregion

        #region ShouldExecuteVsixAnalyzer
        [TestMethod]
        public void ShouldExecuteVsixAnalyzer_WhenSyntaxTreeIsNull_ReturnsFalse()
        {
            // Arrange
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
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
        public void ShouldExecuteVsixAnalyzer_WhenRuleInQualityProfile_ReturnsTrue()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(new QualityProfile(language, new[] { new SonarRule("id1") }));
            var testSubject = CreateTestSubject(boundProject);
            testSubject.LanguageFunc = tree => language;
            var diag1 = CreateFakeDiagnostic(false, "1");
            var diag2 = CreateFakeDiagnostic(false, "2");
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
        public void ShouldExecuteVsixAnalyzer_WhenRuleNotInQualityProfile_ReturnsFalse()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(new QualityProfile(language, new[] { new SonarRule("id3") }));
            var testSubject = CreateTestSubject(boundProject);
            testSubject.LanguageFunc = tree => language;
            var diag1 = CreateFakeDiagnostic(true, "1");
            var diag2 = CreateFakeDiagnostic(true, "2");
            var descriptors = new[] { diag1, diag2 }.Select(x => x.Descriptor);
            var analysisRunContextMock = new Mock<IAnalysisRunContext>();
            analysisRunContextMock.SetupGet(x => x.SupportedDiagnostics).Returns(descriptors);

            // Act
            var result = testSubject.ShouldExecuteVsixAnalyzer(analysisRunContextMock.Object);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExecuteVsixAnalyzer_WhenNoQualityProfileAndAnyRuleInSonarWay_ReturnsTrue()
        {
            // Arrange
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, Language.CSharp))
                .Returns(default(QualityProfile));
            var testSubject = CreateTestSubject(boundProject);
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
        public void ShouldExecuteVsixAnalyzer_WhenNoQualityProfileAndNoRuleInSonarWay_ReturnsFalse()
        {
            // Arrange
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, Language.CSharp))
                .Returns(default(QualityProfile));
            var testSubject = CreateTestSubject(boundProject);
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
        #endregion

        #region VsixAnalyzerReportDiagnostic Tests
        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenRuleIsDisabledInQualityProfile_DoesNotCallReportDiagnostic()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(new QualityProfile(language, new[] { new SonarRule("id3") }));
            var testSubject = CreateTestSubject(boundProject);
            testSubject.LanguageFunc = tree => language;
            var diag = CreateFakeDiagnostic(true, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenNoQualityProfileAndRuleIsNotInSonarWay_DoesNotCallReportDiagnostic()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(default(QualityProfile));
            var testSubject = CreateTestSubject(boundProject);
            var diag = CreateFakeDiagnostic(false, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenRuleIsInQualityProfileButIssueSuppressed_DoesNotCallReportDiagnostic()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(new QualityProfile(language, new[] { new SonarRule("id1") }));
            var testSubject = CreateTestSubject(boundProject);
            testSubject.LanguageFunc = tree => language;
            var diag = CreateFakeDiagnostic(true, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);
            this.suppressionHandlerMock.Setup(x => x.ShouldIssueBeReported(It.IsAny<SyntaxTree>(), It.IsAny<Diagnostic>()))
                .Returns(false);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenNoQualityProfileAndRuleInSonarWayButIssueSuppressed_DoesNotCallReportDiagnostic()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(default(QualityProfile));
            var testSubject = CreateTestSubject(boundProject);
            var diag = CreateFakeDiagnostic(false, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);
            this.suppressionHandlerMock.Setup(x => x.ShouldIssueBeReported(It.IsAny<SyntaxTree>(), It.IsAny<Diagnostic>()))
                .Returns(false);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenRuleIsInQualityProfileAndIssueNotSuppressed_CallsReportDiagnostic()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(new QualityProfile(language, new[] { new SonarRule("id1") }));
            var testSubject = CreateTestSubject(boundProject);
            testSubject.LanguageFunc = tree => language;
            var diag = CreateFakeDiagnostic(true, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);
            this.suppressionHandlerMock.Setup(x => x.ShouldIssueBeReported(It.IsAny<SyntaxTree>(), diag))
                .Returns(true);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(diag), Times.Once);
        }

        [TestMethod]
        public void VsixAnalyzerReportDiagnostic_WhenNoQualityProfileAndRuleInSonarWayAndIssueNotSuppressed_CallsReportDiagnostic()
        {
            // Arrange
            var language = Language.CSharp;
            var boundProject = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
            this.qualityProfileProviderMock.Setup(x => x.GetQualityProfile(boundProject, language))
                .Returns(default(QualityProfile));
            var testSubject = CreateTestSubject(boundProject);
            var diag = CreateFakeDiagnostic(true, "1");
            var reportingContextMock = new Mock<IReportingContext>();
            reportingContextMock.SetupGet(x => x.SyntaxTree).Returns(new Mock<SyntaxTree>().Object);
            reportingContextMock.SetupGet(x => x.Diagnostic).Returns(diag);
            this.suppressionHandlerMock.Setup(x => x.ShouldIssueBeReported(It.IsAny<SyntaxTree>(), diag))
                .Returns(true);

            // Act
            testSubject.VsixAnalyzerReportDiagnostic(reportingContextMock.Object);

            // Assert
            reportingContextMock.Verify(x => x.ReportDiagnostic(diag), Times.Once);
        }
        #endregion

        private TestableSonarAnalyzerConnectedWorkflow CreateTestSubject(BoundSonarQubeProject boundProject = null) =>
            new TestableSonarAnalyzerConnectedWorkflow(qualityProfileProviderMock.Object,
                boundProject ?? new BoundSonarQubeProject { ProjectKey = "ProjectKey" }, suppressionHandlerMock.Object);

        private Diagnostic CreateFakeDiagnostic(bool isInSonarWay = false, string suffix = "") =>
            Diagnostic.Create($"id{suffix}", $"category{suffix}", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning,
                true, 1, customTags: isInSonarWay ? new[] { "SonarWay" } : Enumerable.Empty<string>());
    }
}
