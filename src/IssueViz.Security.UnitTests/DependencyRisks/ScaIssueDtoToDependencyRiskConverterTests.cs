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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using DependencyRiskStatus = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskStatus;
using DependencyRiskTransition = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskTransition;
using DependencyRiskType = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskType;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class ScaIssueDtoToDependencyRiskConverterTests
{
    private ScaIssueDtoToDependencyRiskConverter testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new ScaIssueDtoToDependencyRiskConverter();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<ScaIssueDtoToDependencyRiskConverter, IScaIssueDtoToDependencyRiskConverter>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ScaIssueDtoToDependencyRiskConverter>();

    [TestMethod]
    public void Convert_ReturnsCorrectlyConvertedDependencyRisk()
    {
        var testId = Guid.NewGuid();
        const string packageName = "TestPackage";
        const string packageVersion = "1.2.4";
        const string vulnerabilityId = "CVE-2023-12345";
        const string cvssScore = "7.5";
        var dto = new DependencyRiskDto(
            testId,
            DependencyRiskType.VULNERABILITY,
            DependencyRiskSeverity.HIGH,
            DependencyRiskStatus.CONFIRM,
            packageName,
            packageVersion,
            vulnerabilityId,
            cvssScore,
            [DependencyRiskTransition.CONFIRM, DependencyRiskTransition.REOPEN]);

        var result = testSubject.Convert(dto);

        result.Should().BeEquivalentTo(
            new DependencyRisk(
                testId,
                Core.Analysis.DependencyRiskType.Vulnerability,
                DependencyRiskImpactSeverity.High,
                Core.Analysis.DependencyRiskStatus.Confirmed,
                packageName,
                packageVersion,
                vulnerabilityId,
                cvssScore,
                [Core.Analysis.DependencyRiskTransition.Confirm, Core.Analysis.DependencyRiskTransition.Reopen]
            ));
    }
}
