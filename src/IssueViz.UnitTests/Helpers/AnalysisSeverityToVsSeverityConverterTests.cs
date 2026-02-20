/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Helpers
{
    [TestClass]
    public class AnalysisSeverityToVsSeverityConverterTests
    {
        private const string ProjectName = "MyProject";

        private Mock<IEnvironmentSettings> envSettingsMock;
        private Mock<ITreatWarningsAsErrorsCache> treatWarningsAsErrorsCacheMock;
        private AnalysisSeverityToVsSeverityConverter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            envSettingsMock = new Mock<IEnvironmentSettings>();
            treatWarningsAsErrorsCacheMock = new Mock<ITreatWarningsAsErrorsCache>();
            testSubject = new AnalysisSeverityToVsSeverityConverter(envSettingsMock.Object, treatWarningsAsErrorsCacheMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<AnalysisSeverityToVsSeverityConverter, IAnalysisSeverityToVsSeverityConverter>(
                MefTestHelpers.CreateExport<ITreatWarningsAsErrorsCache>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AnalysisSeverityToVsSeverityConverter>();

        [TestMethod]
        [DataRow(AnalysisIssueSeverity.Info, __VSERRORCATEGORY.EC_MESSAGE)]
        [DataRow(AnalysisIssueSeverity.Minor, __VSERRORCATEGORY.EC_MESSAGE)]
        [DataRow(AnalysisIssueSeverity.Major, __VSERRORCATEGORY.EC_WARNING)]
        [DataRow(AnalysisIssueSeverity.Critical, __VSERRORCATEGORY.EC_WARNING)]
        public void Convert_NotBlocker_CorrectlyMapped(AnalysisIssueSeverity severity, __VSERRORCATEGORY expectedVsErrorCategory)
        {
            testSubject.Convert(severity, ProjectName).Should().Be(expectedVsErrorCategory);
        }

        [TestMethod]
        [DataRow(true, __VSERRORCATEGORY.EC_ERROR)]
        [DataRow(false, __VSERRORCATEGORY.EC_WARNING)]
        public void Convert_Blocker_CorrectlyMapped(bool shouldTreatBlockerAsError, __VSERRORCATEGORY expectedVsErrorCategory)
        {
            envSettingsMock.Setup(x => x.TreatBlockerSeverityAsError()).Returns(shouldTreatBlockerAsError);

            testSubject.Convert(AnalysisIssueSeverity.Blocker, ProjectName).Should().Be(expectedVsErrorCategory);
        }

        [TestMethod]
        [DataRow(SoftwareQualitySeverity.High, __VSERRORCATEGORY.EC_WARNING)]
        [DataRow(SoftwareQualitySeverity.Medium, __VSERRORCATEGORY.EC_WARNING)]
        [DataRow(SoftwareQualitySeverity.Low, __VSERRORCATEGORY.EC_MESSAGE)]
        public void ConvertFromCct_CorrectlyMapped(SoftwareQualitySeverity severity, __VSERRORCATEGORY expectedVsErrorCategory)
        {
            testSubject.ConvertFromCct(severity, ProjectName).Should().Be(expectedVsErrorCategory);
        }

        [TestMethod]
        public void ConvertFromCct_InvalidCctSeverity_DoesNotThrow()
        {
            testSubject.ConvertFromCct((SoftwareQualitySeverity)(-999), ProjectName).Should().Be(__VSERRORCATEGORY.EC_MESSAGE);
        }

        [TestMethod]
        public void Convert_InvalidDaemonSeverity_DoesNotThrow()
        {
            testSubject.Convert((AnalysisIssueSeverity)(-999), ProjectName).Should().Be(__VSERRORCATEGORY.EC_MESSAGE);
        }

        [TestMethod]
        public void GetVsSeverity_IssueWithNewCct_UsesNewCctConverter()
        {
            var converter = new Mock<IAnalysisSeverityToVsSeverityConverter>();

            converter.Object.GetVsSeverity(new DummyAnalysisIssue
            {
                Severity = AnalysisIssueSeverity.Major, HighestImpact = new Impact(SoftwareQuality.Maintainability, SoftwareQualitySeverity.High)
            }, ProjectName);

            converter.Verify(x => x.ConvertFromCct(SoftwareQualitySeverity.High, ProjectName), Times.Once);
            converter.Invocations.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetVsSeverity_IssueWithoutNewCct_UsesOldSeverityConverter()
        {
            var converter = new Mock<IAnalysisSeverityToVsSeverityConverter>();

            converter.Object.GetVsSeverity(new DummyAnalysisIssue
            {
                Severity = AnalysisIssueSeverity.Major
            }, ProjectName);

            converter.Verify(x => x.Convert(AnalysisIssueSeverity.Major, ProjectName), Times.Once);
            converter.Invocations.Should().HaveCount(1);
        }

        [TestMethod]
        [DataRow(AnalysisIssueSeverity.Major, __VSERRORCATEGORY.EC_ERROR)]
        [DataRow(AnalysisIssueSeverity.Critical, __VSERRORCATEGORY.EC_ERROR)]
        public void Convert_TreatWarningsAsErrorsEnabled_WarningBecomesError(AnalysisIssueSeverity severity, __VSERRORCATEGORY expectedVsErrorCategory)
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);

            testSubject.Convert(severity, ProjectName).Should().Be(expectedVsErrorCategory);
        }

        [TestMethod]
        [DataRow(AnalysisIssueSeverity.Info, __VSERRORCATEGORY.EC_MESSAGE)]
        [DataRow(AnalysisIssueSeverity.Minor, __VSERRORCATEGORY.EC_MESSAGE)]
        public void Convert_TreatWarningsAsErrorsEnabled_MessageNotAffected(AnalysisIssueSeverity severity, __VSERRORCATEGORY expectedVsErrorCategory)
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);

            testSubject.Convert(severity, ProjectName).Should().Be(expectedVsErrorCategory);
        }

        [TestMethod]
        public void Convert_TreatWarningsAsErrorsEnabled_BlockerAlreadyError_RemainsError()
        {
            envSettingsMock.Setup(x => x.TreatBlockerSeverityAsError()).Returns(true);
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);

            testSubject.Convert(AnalysisIssueSeverity.Blocker, ProjectName).Should().Be(__VSERRORCATEGORY.EC_ERROR);
        }

        [TestMethod]
        public void Convert_TreatWarningsAsErrorsEnabled_BlockerIsWarning_BecomesError()
        {
            envSettingsMock.Setup(x => x.TreatBlockerSeverityAsError()).Returns(false);
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);

            testSubject.Convert(AnalysisIssueSeverity.Blocker, ProjectName).Should().Be(__VSERRORCATEGORY.EC_ERROR);
        }

        [TestMethod]
        [DataRow(SoftwareQualitySeverity.High, __VSERRORCATEGORY.EC_ERROR)]
        [DataRow(SoftwareQualitySeverity.Medium, __VSERRORCATEGORY.EC_ERROR)]
        [DataRow(SoftwareQualitySeverity.Blocker, __VSERRORCATEGORY.EC_ERROR)]
        public void ConvertFromCct_TreatWarningsAsErrorsEnabled_WarningBecomesError(SoftwareQualitySeverity severity, __VSERRORCATEGORY expectedVsErrorCategory)
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);

            testSubject.ConvertFromCct(severity, ProjectName).Should().Be(expectedVsErrorCategory);
        }

        [TestMethod]
        [DataRow(SoftwareQualitySeverity.Info, __VSERRORCATEGORY.EC_MESSAGE)]
        [DataRow(SoftwareQualitySeverity.Low, __VSERRORCATEGORY.EC_MESSAGE)]
        public void ConvertFromCct_TreatWarningsAsErrorsEnabled_MessageNotAffected(SoftwareQualitySeverity severity, __VSERRORCATEGORY expectedVsErrorCategory)
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);

            testSubject.ConvertFromCct(severity, ProjectName).Should().Be(expectedVsErrorCategory);
        }

        [TestMethod]
        public void GetVsSeverity_TreatWarningsAsErrorsEnabled_WarningBecomesError()
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);
            var issue = new DummyAnalysisIssue { Severity = AnalysisIssueSeverity.Major };

            var result = testSubject.GetVsSeverity(issue, ProjectName);

            result.Should().Be(__VSERRORCATEGORY.EC_ERROR);
        }

        [TestMethod]
        public void GetVsSeverity_TreatWarningsAsErrorsDisabled_WarningRemainsWarning()
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(false);
            var issue = new DummyAnalysisIssue { Severity = AnalysisIssueSeverity.Major };

            var result = testSubject.GetVsSeverity(issue, ProjectName);

            result.Should().Be(__VSERRORCATEGORY.EC_WARNING);
        }

        [TestMethod]
        public void GetVsSeverity_NullCache_WarningRemainsWarning()
        {
            var converterWithNullCache = new AnalysisSeverityToVsSeverityConverter(envSettingsMock.Object, null);
            var issue = new DummyAnalysisIssue { Severity = AnalysisIssueSeverity.Major };

            var result = converterWithNullCache.GetVsSeverity(issue, ProjectName);

            result.Should().Be(__VSERRORCATEGORY.EC_WARNING);
        }

        [TestMethod]
        public void GetVsSeverity_IssueWithHighestImpact_UsesCctConversion()
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(ProjectName)).Returns(true);
            var issue = new DummyAnalysisIssue
            {
                Severity = AnalysisIssueSeverity.Minor, // Would be MESSAGE
                HighestImpact = new Impact(SoftwareQuality.Maintainability, SoftwareQualitySeverity.High) // Would be WARNING, then ERROR
            };

            var result = testSubject.GetVsSeverity(issue, ProjectName);

            result.Should().Be(__VSERRORCATEGORY.EC_ERROR);
        }

        [TestMethod]
        public void GetVsSeverity_NullProjectName_PassesNullToCache()
        {
            treatWarningsAsErrorsCacheMock.Setup(c => c.IsTreatWarningsAsErrorsEnabled(null)).Returns(false);
            var issue = new DummyAnalysisIssue { Severity = AnalysisIssueSeverity.Major };

            var result = testSubject.GetVsSeverity(issue, null);

            result.Should().Be(__VSERRORCATEGORY.EC_WARNING);
            treatWarningsAsErrorsCacheMock.Verify(c => c.IsTreatWarningsAsErrorsEnabled(null), Times.Once);
        }
    }
}
