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
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Rules;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerConnectedWorkflowTests
    {
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
        public void GetLanguage_WhenFirstDescriptorContainsCSharp_ReturnsCSharp()
        {
            // Arrange
            var descriptors = new[] { CreateFakeDiagnostic(LanguageNames.CSharp).Descriptor };

            // Act
            var result = SonarAnalyzerConnectedWorkflow.GetLanguage(descriptors);

            // Assert
            result.Should().Be(Language.CSharp);
        }

        [TestMethod]
        public void GetLanguage_WhenFirstDescriptorContainsVB_ReturnsVB()
        {
            // Arrange
            var descriptors = new[] { CreateFakeDiagnostic(LanguageNames.VisualBasic).Descriptor };

            // Act
            var result = SonarAnalyzerConnectedWorkflow.GetLanguage(descriptors);

            // Assert
            result.Should().Be(Language.VBNET);
        }

        [TestMethod]
        public void GetLanguage_WhenDescriptorsIsNull_ReturnsUnknown()
        {
            using (new AssertIgnoreScope())
            {
                // Act
                var result = SonarAnalyzerConnectedWorkflow.GetLanguage(null);

                // Assert
                result.Should().Be(Language.Unknown);
            }
        }

        [TestMethod]
        public void GetLanguage_WhenNoDescriptor_ReturnsUnknown()
        {
            using (new AssertIgnoreScope())
            {
                // Act
                var result = SonarAnalyzerConnectedWorkflow.GetLanguage(Enumerable.Empty<DiagnosticDescriptor>());

                // Assert
                result.Should().Be(Language.Unknown);
            }
        }

        [TestMethod]
        public void GetLanguage_WhenDescriptorIsNeitherCSharpNorVb_ReturnsUnknown()
        {
            using (new AssertIgnoreScope())
            {
                // Arrange
                var descriptors = new[] { CreateFakeDiagnostic("foo").Descriptor };

                // Act
                var result = SonarAnalyzerConnectedWorkflow.GetLanguage(descriptors);

                // Assert
                result.Should().Be(Language.Unknown);
            }
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
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, true, "1");
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
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, false, "1");
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
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, true, "1");
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
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, false, "1");
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
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, true, "1");
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
            var diag = CreateFakeDiagnostic(LanguageNames.CSharp, true, "1");
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

        private SonarAnalyzerConnectedWorkflow CreateTestSubject(BoundSonarQubeProject boundProject = null) =>
            new SonarAnalyzerConnectedWorkflow(new AdhocWorkspace(), qualityProfileProviderMock.Object,
                boundProject ?? new BoundSonarQubeProject { ProjectKey = "ProjectKey" }, suppressionHandlerMock.Object);

        private Diagnostic CreateFakeDiagnostic(string language, bool isInSonarWay = false, string suffix = "")
        {
            var tags = new List<string> { language };
            if (isInSonarWay)
            {
                tags.Add("SonarWay");
            }

            return Diagnostic.Create($"id{suffix}", $"category{suffix}", "message", DiagnosticSeverity.Warning,
                DiagnosticSeverity.Warning, true, 1, customTags: tags);
        }
    }
}
