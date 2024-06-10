/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers
{
    [TestClass]
    public class ModelConversionExtensionsTests
    {
        [DataRow(IssueSeverity.BLOCKER, AnalysisIssueSeverity.Blocker)]
        [DataRow(IssueSeverity.CRITICAL, AnalysisIssueSeverity.Critical)]
        [DataRow(IssueSeverity.MAJOR, AnalysisIssueSeverity.Major)]
        [DataRow(IssueSeverity.MINOR, AnalysisIssueSeverity.Minor)]
        [DataRow(IssueSeverity.INFO, AnalysisIssueSeverity.Info)]
        [TestMethod]
        public void ToAnalysisIssueSeverity_ConvertsCorrectly(IssueSeverity issueSeverity, AnalysisIssueSeverity excpectedAnalysisIssueSeverity)
        {
            issueSeverity.ToAnalysisIssueSeverity().Should().Be(excpectedAnalysisIssueSeverity);
        }

        [TestMethod]
        public void ToAnalysisIssueSeverity_DoesNotThrow()
        {
            foreach (var issueSeverity in Enum.GetValues(typeof(IssueSeverity)))
            {
                _ = ((IssueSeverity)issueSeverity).ToAnalysisIssueSeverity();
            }
        }
        
        [TestMethod]
        public void ToAnalysisIssueSeverity_ValueOutOfRange_Throws()
        {
            var act = () => ((IssueSeverity)1000).ToAnalysisIssueSeverity();
            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                          Unexpected enum value
                                                                          Parameter name: issueSeverity
                                                                          Actual value was 1000.
                                                                          """);
        }

        [DataRow(RuleType.BUG, AnalysisIssueType.Bug)]
        [DataRow(RuleType.CODE_SMELL, AnalysisIssueType.CodeSmell)]
        [DataRow(RuleType.SECURITY_HOTSPOT, AnalysisIssueType.SecurityHotspot)]
        [DataRow(RuleType.VULNERABILITY, AnalysisIssueType.Vulnerability)]
        [TestMethod]
        public void ToAnalysisIssueType_ConvertsCorrectly(RuleType ruleType, AnalysisIssueType excpectedAnalysisIssueType)
        {
            ruleType.ToAnalysisIssueType().Should().Be(excpectedAnalysisIssueType);
        }

        [TestMethod]
        public void ToAnalysisIssueType_DoesNotThrow()
        {
            foreach (var ruleType in Enum.GetValues(typeof(RuleType)))
            {
                _ = ((RuleType)ruleType).ToAnalysisIssueType();
            }
        }
        
        [TestMethod]
        public void ToAnalysisIssueType_ValueOutOfRange_Throws()
        {
            var act = () => ((RuleType)1000).ToAnalysisIssueType();
            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                          Unexpected enum value
                                                                          Parameter name: ruleType
                                                                          Actual value was 1000.
                                                                          """);
        }

        [DataRow(ImpactSeverity.LOW, SoftwareQualitySeverity.Low)]
        [DataRow(ImpactSeverity.MEDIUM, SoftwareQualitySeverity.Medium)]
        [DataRow(ImpactSeverity.HIGH, SoftwareQualitySeverity.High)]
        [TestMethod]
        public void ToSoftwareQualitySeverity_ConvertsCorrectly(ImpactSeverity impactSeverity, SoftwareQualitySeverity excpectedSoftwareQualitySeverity)
        {
            impactSeverity.ToSoftwareQualitySeverity().Should().Be(excpectedSoftwareQualitySeverity);
        }

        [TestMethod]
        public void ToSoftwareQualitySeverity_DoesNotThrow()
        {
            foreach (var impactSeverity in Enum.GetValues(typeof(ImpactSeverity)))
            {
                _ = ((ImpactSeverity)impactSeverity).ToSoftwareQualitySeverity();
            }
        }
        
        [TestMethod]
        public void ToSoftwareQualitySeverity_ValueOutOfRange_Throws()
        {
            var act = () => ((ImpactSeverity)1000).ToSoftwareQualitySeverity();
            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                          Unexpected enum value
                                                                          Parameter name: impactSeverity
                                                                          Actual value was 1000.
                                                                          """);
        }
    }
}
