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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Rules;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    [TestClass]
    public class SonarAnalyzerManagerTests
    {
        private Mock<IQualityProfileProvider> qualityProfileProviderMock;
        private AdhocWorkspace workspace;
        private ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker;
        private Mock<ILogger> loggerMock;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private Mock<IVsSolution> vsSolutionMock;

        [TestInitialize]
        public void TestInitialize()
        {
            qualityProfileProviderMock = new Mock<IQualityProfileProvider>();
            workspace = new AdhocWorkspace();
            activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();
            loggerMock = new Mock<ILogger>();
            sonarQubeServiceMock = new Mock<ISonarQubeService>();
            vsSolutionMock = new Mock<IVsSolution>();
        }

        #region Ctor
        [TestMethod]
        public void Ctor_WhenIActiveSolutionBoundTrackerIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerManager(null, sonarQubeServiceMock.Object, workspace,
                qualityProfileProviderMock.Object, vsSolutionMock.Object, loggerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionBoundTracker");
        }

        [TestMethod]
        public void Ctor_WhenISonarQubeServiceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerManager(activeSolutionBoundTracker, null, workspace,
                qualityProfileProviderMock.Object, vsSolutionMock.Object, loggerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");
        }

        [TestMethod]
        public void Ctor_WhenVisualStudioWorkspaceIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerManager(activeSolutionBoundTracker, sonarQubeServiceMock.Object, null,
                qualityProfileProviderMock.Object, vsSolutionMock.Object, loggerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("workspace");
        }

        [TestMethod]
        public void Ctor_WhenIQualityProfileProviderIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerManager(activeSolutionBoundTracker, sonarQubeServiceMock.Object,
                workspace, null, vsSolutionMock.Object, loggerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("qualityProfileProvider");
        }

        [TestMethod]
        public void Ctor_WhenIVsSolutionIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerManager(activeSolutionBoundTracker, sonarQubeServiceMock.Object,
                workspace, qualityProfileProviderMock.Object, null, loggerMock.Object);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("vsSolution");
        }

        [TestMethod]
        public void Ctor_WhenILoggerIsNull_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarAnalyzerManager(activeSolutionBoundTracker, sonarQubeServiceMock.Object,
                workspace, qualityProfileProviderMock.Object, vsSolutionMock.Object, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }
        #endregion

        #region VSIX + NuGet handling
        [TestMethod]
        public void SonarAnalyzerManager_HasNoCollidingAnalyzerReference_OnEmptyList()
        {
            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(null))
                .Should().BeFalse("Null analyzer reference list should not report conflicting analyzer packages");

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(new List<AnalyzerReference>()))
                .Should().BeFalse("Empty analyzer reference list should not report conflicting analyzer packages");
        }

        [TestMethod]
        public void SonarAnalyzerManager_HasCollidingAnalyzerReference()
        {
            var version = new Version("0.1.2.3");
            version.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version),
                            SonarAnalyzerManager.AnalyzerName)
            };

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeTrue("Conflicting analyzer package not found");
        }

        [TestMethod]
        public void SonarAnalyzerManager_HasNoCollidingAnalyzerReference_SameNameVersion()
        {
            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, SonarAnalyzerManager.AnalyzerVersion),
                            SonarAnalyzerManager.AnalyzerName)
            };

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Same named and versioned analyzers should not be reported as conflicting ones");
        }

        [TestMethod]
        public void SonarAnalyzerManager_HasNoCollidingAnalyzerReference_SameVersionDifferentName()
        {
            var name = "Some test name";
            name.Should().NotBe(SonarAnalyzerManager.AnalyzerName,
                "Test input should be different from the expected analyzer name");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(name, SonarAnalyzerManager.AnalyzerVersion), name)
            };

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Name is not considered in the conflict checking");
        }

        [TestMethod]
        public void SonarAnalyzerManager_HasNoCollidingAnalyzerReference_NoDisplayName()
        {
            var version = new Version("0.1.2.3");
            version.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version),
                            null)
            };

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Null analyzer name should not report conflict");
        }

        [TestMethod]
        public void SonarAnalyzerManager_HasNoCollidingAnalyzerReference_NoAssemblyIdentity()
        {
            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new object(),
                            SonarAnalyzerManager.AnalyzerName)
            };

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeTrue("If no AssemblyIdentity is present, but the name matches, we should report a conflict");
        }

        [TestMethod]
        public void SonarAnalyzerManager_MultipleReferencesWithSameName_CollidingVersion()
        {
            var version = new Version("0.1.2.3");
            version.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version),
                            SonarAnalyzerManager.AnalyzerName),
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, SonarAnalyzerManager.AnalyzerVersion),
                            SonarAnalyzerManager.AnalyzerName),
            };

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Having already colliding references should not disable the embedded analyzer if one is of the same version");
        }

        [TestMethod]
        public void SonarAnalyzerManager_MultipleReferencesWithSameName_NonCollidingVersion()
        {
            var version1 = new Version("0.1.2.3");
            version1.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
                "Test input should be different from the expected analyzer version");
            var version2 = new Version("1.2.3.4");
            version2.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version1),
                            SonarAnalyzerManager.AnalyzerName),
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version2),
                            SonarAnalyzerManager.AnalyzerName),
            };

            SonarAnalyzerManager.HasConflictingAnalyzerReference(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeTrue("Having only different reference versions should disable the embedded analyzer");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Unbound_Empty()
        {
            this.activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;

            var testSubject = CreateTestSubject();
            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(null))
                .Should().BeFalse("Unbound solution should never return true");

            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(new List<AnalyzerReference>()))
                .Should().BeFalse("Unbound solution should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Unbound_Conflicting()
        {
            this.activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;

            var version = new Version("0.1.2.3");
            version.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version),
                            SonarAnalyzerManager.AnalyzerName)
            };

            var testSubject = CreateTestSubject();
            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Unbound solution should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Unbound_NonConflicting()
        {
            this.activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, SonarAnalyzerManager.AnalyzerVersion),
                            SonarAnalyzerManager.AnalyzerName)
            };

            var testSubject = CreateTestSubject();
            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Unbound solution should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Bound_Empty()
        {
            this.activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new Persistence.BoundSonarQubeProject(), true);

            var testSubject = CreateTestSubject();
            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(null))
                .Should().BeTrue("Bound solution with no reference should never return true");

            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(new List<AnalyzerReference>()))
                .Should().BeTrue("Bound solution with no reference should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Bound_Conflicting()
        {
            this.activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new Persistence.BoundSonarQubeProject(), true);

            var version = new Version("0.1.2.3");
            version.Should().NotBe(SonarAnalyzerManager.AnalyzerVersion,
               "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, version),
                            SonarAnalyzerManager.AnalyzerName)
            };

            var testSubject = CreateTestSubject();
            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Bound solution with conflicting analyzer name should never return true");
        }

        [TestMethod]
        public void SonarAnalyzerManager_GetIsBoundWithoutAnalyzer_Bound_NonConflicting()
        {
            this.activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new Persistence.BoundSonarQubeProject(), true);

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                        new ConfigurableAnalyzerReference(
                            new AssemblyIdentity(SonarAnalyzerManager.AnalyzerName, SonarAnalyzerManager.AnalyzerVersion),
                            SonarAnalyzerManager.AnalyzerName)
            };

            var testSubject = CreateTestSubject();
            testSubject.GetIsBoundWithoutAnalyzer(
                SonarAnalyzerManager.GetProjectAnalyzerConflictStatus(references))
                .Should().BeFalse("Bound solution with conflicting analyzer name should never return true");
        }
        #endregion

        #region Event Raised
        [TestMethod]
        public void Class_WhenSolutionBindingChangedRaisedAndNoLongerBound_DoesTheExpected()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject { ProjectKey = "Foo" }, isLegacy: false);
            qualityProfileProviderMock.Setup(x => x.GetQualityProfile(It.IsAny<BoundSonarQubeProject>(), Language.CSharp))
                .Returns(new QualityProfile(Language.CSharp, new[] { new SonarRule("id1") }));
            var testSubject = CreateTestSubject();

            // Sanity checks
            testSubject.cachedQualityProfiles.Should().NotBeEmpty();
            testSubject.delegateInjector.Should().NotBeNull();
            testSubject.sonarqubeIssueProvider.Should().NotBeNull();

            // Act
            // The class ignores the content of the eventArgs and read again the property so we need to update it
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;
            activeSolutionBoundTracker.SimulateSolutionBindingChanged(null);

            // Assert
            testSubject.cachedQualityProfiles.Should().BeEmpty();
            testSubject.delegateInjector.Should().BeNull();
            testSubject.sonarqubeIssueProvider.Should().BeNull();
        }

        [TestMethod]
        public void Class_WhenSolutionBindingChangedRaisedAndIsBound_DoesTheExpected()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;
            qualityProfileProviderMock.Setup(x => x.GetQualityProfile(It.IsAny<BoundSonarQubeProject>(), Language.CSharp))
                .Returns(new QualityProfile(Language.CSharp, new[] { new SonarRule("id1") }));
            var testSubject = CreateTestSubject();

            // Sanity checks
            testSubject.cachedQualityProfiles.Should().BeEmpty();
            testSubject.delegateInjector.Should().BeNull();
            testSubject.sonarqubeIssueProvider.Should().BeNull();

            // Act
            // The class ignores the content of the eventArgs and read again the property so we need to update it
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject { ProjectKey = "Foo" }, isLegacy: false);
            activeSolutionBoundTracker.SimulateSolutionBindingChanged(null);

            // Assert
            testSubject.cachedQualityProfiles.Should().NotBeEmpty();
            testSubject.delegateInjector.Should().NotBeNull();
            testSubject.sonarqubeIssueProvider.Should().NotBeNull();
        }
        #endregion

        #region Standalone Mode

        [TestMethod]
        public void GetConfiguredDiagnostic_WhenStandalone_ReturnsTheSameDiagnostic()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;
            var testSubject = CreateTestSubject();
            var diagnostic = CreateFakeDiagnostic();

            // Act
            var result = testSubject.GetConfiguredDiagnostic(diagnostic);

            // Assert
            result.Should().Be(diagnostic);
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenCurrentModeIsStandaloneAndRuleIsInSonarWay_ReturnsTrue()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;
            var testSubject = CreateTestSubject();
            var diagnostic1 = CreateFakeDiagnostic(false, "1");
            var diagnostic2 = CreateFakeDiagnostic(true, "2");
            var descriptors = new[] { diagnostic1.Descriptor, diagnostic2.Descriptor };
            int callCount = 0;

            // Act
            var result = testSubject.HasAnyDiagnosticEnabled(descriptors, tree => { callCount++; return null; });

            // Assert
            callCount.Should().Be(0);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenCurrentModeIsStandaloneAndRuleIsNotInSonarWay_ReturnsFalse()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;
            var testSubject = CreateTestSubject();
            var diagnostic1 = CreateFakeDiagnostic(false, "1");
            var diagnostic2 = CreateFakeDiagnostic(false, "2");
            var descriptors = new[] { diagnostic1.Descriptor, diagnostic2.Descriptor };
            int callCount = 0;

            // Act
            var result = testSubject.HasAnyDiagnosticEnabled(descriptors, tree => { callCount++; return null; });

            // Assert
            callCount.Should().Be(0);
            result.Should().BeFalse();
        }

        #endregion

        #region LegacyConnected Mode

        [TestMethod]
        public void GetConfiguredDiagnostic_WhenLegacyConnected_ReturnsTheSameDiagnostic()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(), isLegacy: true);
            var testSubject = CreateTestSubject();
            var diagnostic = CreateFakeDiagnostic();

            // Act
            var result = testSubject.GetConfiguredDiagnostic(diagnostic);

            // Assert
            result.Should().Be(diagnostic);
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenCurrentModeIsLegacyConnected_ReturnsTrue()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(), isLegacy: true);
            var testSubject = CreateTestSubject();
            int callCount = 0;

            // Act
            var result = testSubject.HasAnyDiagnosticEnabled(null, tree => { callCount++; return null; });

            // Assert
            callCount.Should().Be(0);
            result.Should().BeTrue();
        }

        #endregion

        #region Connected Mode

        [TestMethod]
        public void GetConfiguredDiagnostic_WhenConnected_ReturnsTheSameDiagnostic()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(), isLegacy: false);
            var testSubject = CreateTestSubject();
            var diagnostic = CreateFakeDiagnostic();

            // Act
            var result = testSubject.GetConfiguredDiagnostic(diagnostic);

            // Assert
            result.Should().Be(diagnostic);
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenCurrentModeIsConnectedAndRuleIsEnabled_ReturnsTrue()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(), isLegacy: false);
            qualityProfileProviderMock.Setup(x => x.GetQualityProfile(It.IsAny<BoundSonarQubeProject>(), Language.CSharp))
                .Returns(new QualityProfile(Language.CSharp, new[] { new SonarRule("id1"), new SonarRule("id3") }));
            var testSubject = CreateTestSubject();
            var diagnostic1 = CreateFakeDiagnostic(false, "1");
            var diagnostic2 = CreateFakeDiagnostic(false, "2");
            var descriptors = new[] { diagnostic1.Descriptor, diagnostic2.Descriptor };
            int callCount = 0;

            // Act
            var result = testSubject.HasAnyDiagnosticEnabled(descriptors, tree => { callCount++; return Language.CSharp; });

            // Assert
            callCount.Should().Be(1);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenCurrentModeIsConnectedAndRuleIsDisabled_ReturnsFalse()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(), isLegacy: false);
            qualityProfileProviderMock.Setup(x => x.GetQualityProfile(It.IsAny<BoundSonarQubeProject>(), Language.CSharp))
                .Returns(new QualityProfile(Language.CSharp, new[] { new SonarRule("id3"), new SonarRule("id4") }));
            var testSubject = CreateTestSubject();
            var diagnostic1 = CreateFakeDiagnostic(false, "1");
            var diagnostic2 = CreateFakeDiagnostic(false, "2");
            var descriptors = new[] { diagnostic1.Descriptor, diagnostic2.Descriptor };
            int callCount = 0;

            // Act
            var result = testSubject.HasAnyDiagnosticEnabled(descriptors, tree => { callCount++; return Language.CSharp; });

            // Assert
            callCount.Should().Be(1);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenCurrentModeIsConnectedAndRuleKeyNotFoundButRuleInSonarWay_ReturnsTrue()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(), isLegacy: false);
            qualityProfileProviderMock.Setup(x => x.GetQualityProfile(It.IsAny<BoundSonarQubeProject>(), Language.CSharp))
                .Returns(default(QualityProfile));
            var testSubject = CreateTestSubject();
            var diagnostic1 = CreateFakeDiagnostic(false, "1");
            var diagnostic2 = CreateFakeDiagnostic(true, "2");
            var descriptors = new[] { diagnostic1.Descriptor, diagnostic2.Descriptor };
            int callCount = 0;

            // Act
            var result = testSubject.HasAnyDiagnosticEnabled(descriptors, tree => { callCount++; return Language.Unknown; });

            // Assert
            callCount.Should().Be(1);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenCurrentModeIsConnectedAndRuleKeyNotFoundButRuleNotInSonarWay_ReturnsFalse()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject(), isLegacy: false);
            qualityProfileProviderMock.Setup(x => x.GetQualityProfile(It.IsAny<BoundSonarQubeProject>(), It.IsAny<Language>()))
                .Returns(default(QualityProfile));
            var testSubject = CreateTestSubject();
            var diagnostic1 = CreateFakeDiagnostic(false, "1");
            var diagnostic2 = CreateFakeDiagnostic(false, "2");
            var descriptors = new[] { diagnostic1.Descriptor, diagnostic2.Descriptor };
            int callCount = 0;

            // Act
            var result = testSubject.HasAnyDiagnosticEnabled(descriptors, tree => { callCount++; return Language.Unknown; });

            // Assert
            callCount.Should().Be(1);
            result.Should().BeFalse();
        }

        #endregion

        #region Others
        [TestMethod]
        public void Dispose_DoesTheExpected()
        {
            // Arrange
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.CreateBoundConfiguration(
                new BoundSonarQubeProject { ProjectKey = "Foo" }, isLegacy: false);
            qualityProfileProviderMock.Setup(x => x.GetQualityProfile(It.IsAny<BoundSonarQubeProject>(), Language.CSharp))
                .Returns(new QualityProfile(Language.CSharp, new[] { new SonarRule("id1") }));
            vsSolutionMock.As<IVsSolution5>(); // Allows to cast IVsSolution into IVsSolution5
            var testSubject = CreateTestSubject();

            // Sanity checks
            testSubject.cachedQualityProfiles.Should().NotBeEmpty();
            testSubject.delegateInjector.Should().NotBeNull();
            testSubject.sonarqubeIssueProvider.Should().NotBeNull();
            SonarAnalysisContext.ShouldExecuteRuleFunc.Should().NotBeNull();
            SonarAnalysisContext.ReportDiagnosticAction.Should().NotBeNull();

            // Act
            testSubject.Dispose();

            // Assert
            testSubject.cachedQualityProfiles.Should().BeEmpty();
            testSubject.delegateInjector.Should().BeNull();
            testSubject.sonarqubeIssueProvider.Should().BeNull();
            SonarAnalysisContext.ShouldExecuteRuleFunc.Should().BeNull();
            SonarAnalysisContext.ReportDiagnosticAction.Should().BeNull();
        }

        [TestMethod]
        public void HasAnyDiagnosticEnabled_WhenInvalidSonarLintMode_ReturnsFalse()
        {
            // Arrange
            this.activeSolutionBoundTracker.CurrentConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), (SonarLintMode) 10);
            var testSubject = CreateTestSubject();
            var callCount = 0;

            // Act
            bool result;
            using (new AssertIgnoreScope())
            {
                result = testSubject.HasAnyDiagnosticEnabled(null, tree => { callCount++; return null; });
            }

            // Assert
            callCount.Should().Be(0);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void GetLanguage_WhenCSharpSyntaxTree_ReturnsCSharp()
        {
            // Arrange
            var syntaxTree = CSharpSyntaxTree.ParseText("public class Foo {}");

            // Act
            var result = SonarAnalyzerManager.GetLanguage(syntaxTree);

            // Assert
            result.Should().Be(Language.CSharp);
        }

        [TestMethod]
        public void GetLanguage_WhenVisualBasicSyntaxTree_ReturnsVBNet()
        {
            // Arrange
            var syntaxTree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText(@"Module Program
End Module");

            // Act
            var result = SonarAnalyzerManager.GetLanguage(syntaxTree);

            // Assert
            result.Should().Be(Language.VBNET);
        }

        [TestMethod]
        public void GetLanguage_WhenSyntaxTreeIsNull_ReturnsUnknown()
        {
            // Arrange & Act
            var result = SonarAnalyzerManager.GetLanguage(null);

            // Assert
            result.Should().Be(Language.Unknown);
        }

        [TestMethod]
        public void GetLanguage_WhenSyntaxTreeRootIsNull_ReturnsUnknown()
        {
            // Arrange
            var syntaxTree = new Mock<SyntaxTree>();

            // Act
            var result = SonarAnalyzerManager.GetLanguage(syntaxTree.Object);

            // Assert
            result.Should().Be(Language.Unknown);
        }
        #endregion

        private Diagnostic CreateFakeDiagnostic(bool isInSonarWay = false, string suffix = "") =>
            Diagnostic.Create($"id{suffix}", $"category{suffix}", "message", DiagnosticSeverity.Warning,
                DiagnosticSeverity.Warning, true, 1, customTags: isInSonarWay ? new[] { "SonarWay" } : Enumerable.Empty<string>());

        private SonarAnalyzerManager CreateTestSubject() => new SonarAnalyzerManager(activeSolutionBoundTracker,
            sonarQubeServiceMock.Object, workspace, qualityProfileProviderMock.Object, vsSolutionMock.Object, loggerMock.Object);
    }
}