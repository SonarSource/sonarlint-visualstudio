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
using SonarLint.VisualStudio.Integration.Vsix;
using static SonarLint.VisualStudio.Integration.Vsix.SonarAnalyzerWorkflowBase;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerWorkflowBaseTests
    {
        private class TestableSonarAnalyzerWorkflow : SonarAnalyzerWorkflowBase
        {
            public Func<SyntaxTree, ProjectAnalyzerStatus> GetProjectNuGetAnalyzerStatusFunc { get; set; }
            public Func<IEnumerable<DiagnosticDescriptor>, bool?> ShouldRegisterContextActionFunc { get; set; }

            public TestableSonarAnalyzerWorkflow(Workspace workspace)
                : base(workspace)
            {
            }

            protected override ProjectAnalyzerStatus GetProjectNuGetAnalyzerStatus(SyntaxTree syntaxTree) =>
                GetProjectNuGetAnalyzerStatusFunc(syntaxTree);

            protected internal override bool? ShouldRegisterContextAction(IEnumerable<DiagnosticDescriptor> descriptors) =>
                ShouldRegisterContextActionFunc?.Invoke(descriptors);
        }

        [TestMethod]
        public void Ctor_WhenWorkspaceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new TestableSonarAnalyzerWorkflow(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_SetsTheExpectedDelegates()
        {
            // Arrange
            Func<IEnumerable<DiagnosticDescriptor>, bool> expectedShouldRegisterContextAction = list => false;
            SonarAnalysisContext.ShouldRegisterContextAction = expectedShouldRegisterContextAction;
            Func<SyntaxTree, bool> expectedShouldExecuteRegisteredAction = tree => true;
            SonarAnalysisContext.ShouldExecuteRegisteredAction = expectedShouldExecuteRegisteredAction;

            // Act
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());

            // Assert
            SonarAnalysisContext.ShouldRegisterContextAction.Should().NotBe(expectedShouldRegisterContextAction);
            SonarAnalysisContext.ShouldExecuteRegisteredAction.Should().NotBe(expectedShouldExecuteRegisteredAction);
        }

        [TestMethod]
        public void ShouldExecuteRegisteredAction_WhenTreeIsNull_ReturnsFalse()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());

            // Act
            var result = testSubject.ShouldExecuteRegisteredAction(null);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExecuteRegisteredAction_WhenTreeNotNullAndNoNuGetAnalyzer_ReturnsTrue()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());
            testSubject.GetProjectNuGetAnalyzerStatusFunc = tree => ProjectAnalyzerStatus.NoAnalyzer;

            // Act
            var result = testSubject.ShouldExecuteRegisteredAction(new Mock<SyntaxTree>().Object);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldExecuteRegisteredAction_WhenTreeNotNullAndNuGetAnalyzerSameVersion_ReturnsFalse()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());
            testSubject.GetProjectNuGetAnalyzerStatusFunc = tree => ProjectAnalyzerStatus.SameVersion;

            // Act
            var result = testSubject.ShouldExecuteRegisteredAction(new Mock<SyntaxTree>().Object);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExecuteRegisteredAction_WhenTreeNotNullAndNuGetAnalyzerDifferentVersion_ReturnsFalse()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());
            testSubject.GetProjectNuGetAnalyzerStatusFunc = tree => ProjectAnalyzerStatus.DifferentVersion;

            // Act
            var result = testSubject.ShouldExecuteRegisteredAction(new Mock<SyntaxTree>().Object);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldRegisterContextAction_AlwaysReturnsNull()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());

            // Act
            var result1 = testSubject.ShouldRegisterContextAction(null);
            var result2 = testSubject.ShouldRegisterContextAction(Enumerable.Empty<DiagnosticDescriptor>());

            // Assert
            result1.Should().BeNull();
            result2.Should().BeNull();
        }

        [TestMethod]
        public void ShouldRegisterContextActionWithFallback_WhenAnyDescriptorNotConfigurable_ReturnsTrue()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());
            var diag1 = CreateFakeDiagnostic(false, "1", isNotConfigurable: false);
            var diag2 = CreateFakeDiagnostic(false, "2", isNotConfigurable: true);
            var descriptors = new[] { diag1, diag2 }.Select(x => x.Descriptor);

            // Act
            var result = testSubject.ShouldRegisterContextActionWithFallback(descriptors);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldRegisterContextActionWithFallback_WhenShouldRegisterContextActionReturnsNullAndRuleInSonarWay_ReturnsTrue()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());
            var diag1 = CreateFakeDiagnostic(false, "1");
            var diag2 = CreateFakeDiagnostic(true, "2");
            var descriptors = new[] { diag1, diag2 }.Select(x => x.Descriptor);

            // Act
            var result = testSubject.ShouldRegisterContextActionWithFallback(descriptors);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldRegisterContextActionWithFallback_WhenShouldRegisterContextActionReturnsNullAndRuleNotInSonarWay_ReturnsFalse()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());
            var diag1 = CreateFakeDiagnostic(false, "1");
            var diag2 = CreateFakeDiagnostic(false, "2");
            var descriptors = new[] { diag1, diag2 }.Select(x => x.Descriptor);

            // Act
            var result = testSubject.ShouldRegisterContextActionWithFallback(descriptors);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldRegisterContextActionWithFallback_WhenShouldRegisterContextActionDoesNotReturnNull_ReturnsSameResult()
        {
            // Arrange
            var testSubject = new TestableSonarAnalyzerWorkflow(new AdhocWorkspace());
            var diag1 = CreateFakeDiagnostic(true, "1");
            var diag2 = CreateFakeDiagnostic(true, "2");
            var descriptors = new[] { diag1, diag2 }.Select(x => x.Descriptor);
            testSubject.ShouldRegisterContextActionFunc = d => false;

            // Act
            var result = testSubject.ShouldRegisterContextActionWithFallback(descriptors);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenReferencesIsNull_ReturnsNoAnalyzer()
        {
            // Arrange & Act
            var result = ProcessAnalyzerReferences(null);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.NoAnalyzer);
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenNoReferencesMatchName_ReturnsNoAnalyzer()
        {
            // Arrange
            var references = new[] { new ConfigurableAnalyzerReference(null, "foo1") };

            // Act
            var result = ProcessAnalyzerReferences(references);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.NoAnalyzer);
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenNameMatchesAndDifferentVersions_ReturnsDifferentVersion()
        {
            // Arrange
            var references = new[] { new ConfigurableAnalyzerReference(new AssemblyIdentity(AnalyzerName, new Version("0.1.2.3")),
                AnalyzerName) };

            // Act
            var result = ProcessAnalyzerReferences(references);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.DifferentVersion);
        }

        [TestMethod]
        public void ProcessAnalyzerReferences_WhenNameMatchesAndSameVersions_ReturnsSameVersion()
        {
            // Arrange
            var references = new[] { new ConfigurableAnalyzerReference(new AssemblyIdentity(AnalyzerName, AnalyzerVersion),
                AnalyzerName) };

            // Act
            var result = ProcessAnalyzerReferences(references);

            // Assert
            result.Should().Be(ProjectAnalyzerStatus.SameVersion);
        }

        private Diagnostic CreateFakeDiagnostic(bool isInSonarWay = false, string suffix = "", bool isNotConfigurable = false)
        {
            var tags = new List<string>();

            if (isInSonarWay)
            {
                tags.Add("SonarWay");
            }
            if (isNotConfigurable)
            {
                tags.Add(WellKnownDiagnosticTags.NotConfigurable);
            }

            return Diagnostic.Create($"id{suffix}", $"category{suffix}", "message", DiagnosticSeverity.Warning,
                DiagnosticSeverity.Warning, true, 1, customTags: tags.ToArray());
        }
    }
}
