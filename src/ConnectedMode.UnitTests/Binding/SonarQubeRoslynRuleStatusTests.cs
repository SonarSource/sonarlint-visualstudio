/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client.Models;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding;

[TestClass]
public class SonarQubeRoslynRuleStatusTests
{
    private IEnvironmentSettings environmentSettings;

    [TestInitialize]
    public void TestInitialize()
    {
        environmentSettings = Substitute.For<IEnvironmentSettings>();
    }

    [TestMethod]
    [DataRow(SonarQubeIssueSeverity.Info, RuleAction.Info)]
    [DataRow(SonarQubeIssueSeverity.Minor, RuleAction.Info)]
    [DataRow(SonarQubeIssueSeverity.Major, RuleAction.Warning)]
    [DataRow(SonarQubeIssueSeverity.Critical, RuleAction.Warning)]
    public void GetVSSeverity_NotBlocker_CorrectlyMapped(SonarQubeIssueSeverity sqSeverity, RuleAction expectedVsSeverity)
    {
        var testSubject = new SonarQubeRoslynRuleStatus(CreateStandardRule(sqSeverity), environmentSettings);

        testSubject.GetSeverity().Should().Be(expectedVsSeverity);
    }

    [TestMethod]
    [DataRow(true, RuleAction.Error)]
    [DataRow(false, RuleAction.Warning)]
    public void GetVSSeverity_Blocker_CorrectlyMapped(bool shouldTreatBlockerAsError, RuleAction expectedVsSeverity)
    {
        environmentSettings.TreatBlockerSeverityAsError().Returns(shouldTreatBlockerAsError);

        var testSubject = new SonarQubeRoslynRuleStatus(CreateStandardRule(SonarQubeIssueSeverity.Blocker), environmentSettings);

        testSubject.GetSeverity().Should().Be(expectedVsSeverity);
    }

    [TestMethod]
    [DataRow(SonarQubeIssueSeverity.Unknown)]
    [DataRow((SonarQubeIssueSeverity)(-1))]
    public void GetVSSeverity_Invalid_Throws(SonarQubeIssueSeverity sqSeverity)
    {
        var testSubject = new SonarQubeRoslynRuleStatus(CreateStandardRule(sqSeverity), environmentSettings);

        Action act = () => testSubject.GetSeverity();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [DataTestMethod]
    [DataRow(SonarQubeSoftwareQualitySeverity.Info, RuleAction.Info)]
    [DataRow(SonarQubeSoftwareQualitySeverity.Low, RuleAction.Info)]
    [DataRow(SonarQubeSoftwareQualitySeverity.Medium, RuleAction.Warning)]
    public void GetVSSeverity_FromSoftwareQualitySeverity_NotBlocker_CorrectlyMapped(SonarQubeSoftwareQualitySeverity sqSeverity, RuleAction expectedVsSeverity)
    {
        var testSubject = new SonarQubeRoslynRuleStatus(
            CreateMqrRule(sqSeverity),
            environmentSettings);

        testSubject.GetSeverity().Should().Be(expectedVsSeverity);
    }

    [TestMethod]
    [DataRow(SonarQubeSoftwareQualitySeverity.High, true, RuleAction.Error)]
    [DataRow(SonarQubeSoftwareQualitySeverity.High, false, RuleAction.Warning)]
    [DataRow(SonarQubeSoftwareQualitySeverity.Blocker, true, RuleAction.Error)]
    [DataRow(SonarQubeSoftwareQualitySeverity.Blocker, false, RuleAction.Warning)]
    public void GetVSSeverity_FromSoftwareQualitySeverity_Blocker_CorrectlyMapped(SonarQubeSoftwareQualitySeverity sqSeverity, bool shouldTreatBlockerAsError, RuleAction expectedVsSeverity)
    {
        environmentSettings.TreatBlockerSeverityAsError().Returns(shouldTreatBlockerAsError);

        var testSubject = new SonarQubeRoslynRuleStatus(CreateMqrRule(sqSeverity), environmentSettings);

        testSubject.GetSeverity().Should().Be(expectedVsSeverity);
    }

    [TestMethod]
    public void GetVSSeverity_FromSoftwareQualitySeverity_Invalid_Throws()
    {
        var testSubject = new SonarQubeRoslynRuleStatus(CreateMqrRule((SonarQubeSoftwareQualitySeverity)(-1)), environmentSettings);

        Action act = () => testSubject.GetSeverity();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [DynamicData(nameof(MultipleMqrSeveritiesAndHighestConvertedVsSeverity))]
    [DataTestMethod]
    public void GetVSSeverity_FromSoftwareQualitySeverity_Multiple_TakesHighest(SonarQubeSoftwareQualitySeverity[] severities, RuleAction expectedAction)
    {
        var testSubject = new SonarQubeRoslynRuleStatus(CreateMqrRule(severities), environmentSettings);

        testSubject.GetSeverity().Should().Be(expectedAction);
    }

    [DataRow(SonarQubeIssueSeverity.Info, RuleAction.Info)]
    [DataRow(SonarQubeIssueSeverity.Minor, RuleAction.Info)]
    [DataRow(SonarQubeIssueSeverity.Major, RuleAction.Warning)]
    [DataRow(SonarQubeIssueSeverity.Critical, RuleAction.Warning)]
    [DataTestMethod]
    public void GetVSSeverity_EmptyMqrSeverities_UsesStandardSeverity(SonarQubeIssueSeverity sqSeverity, RuleAction expectedVsSeverity)
    {
        var testSubject = new SonarQubeRoslynRuleStatus(CreateRule(new(), sqSeverity), environmentSettings);

        testSubject.GetSeverity().Should().Be(expectedVsSeverity);
    }

    [DynamicData(nameof(AllMqrSeverities))]
    [DataTestMethod]
    public void GetVSSeverity_HasSoftwareQualitySeverity_Inactive_ReturnsNone(SonarQubeSoftwareQualitySeverity severities)
    {
        var testSubject = new SonarQubeRoslynRuleStatus(CreateMqrRule(isActive: false, severities), environmentSettings);

        testSubject.GetSeverity().Should().Be(RuleAction.None);
    }

    [DynamicData(nameof(AllSeverities))]
    [DataTestMethod]
    public void GetVSSeverity_HasSeverity_Inactive_ReturnsNone(SonarQubeIssueSeverity severity)
    {
        var testSubject = new SonarQubeRoslynRuleStatus(CreateStandardRule(severity, isActive: false), environmentSettings);

        testSubject.GetSeverity().Should().Be(RuleAction.None);
    }

    public static object[][] MultipleMqrSeveritiesAndHighestConvertedVsSeverity =>
    [
        [new[] { SonarQubeSoftwareQualitySeverity.Blocker, SonarQubeSoftwareQualitySeverity.Low }, RuleAction.Warning],
        [new[] { SonarQubeSoftwareQualitySeverity.Low, SonarQubeSoftwareQualitySeverity.Blocker }, RuleAction.Warning],
        [new[] { SonarQubeSoftwareQualitySeverity.Blocker, SonarQubeSoftwareQualitySeverity.Blocker }, RuleAction.Warning],
        [new[] { SonarQubeSoftwareQualitySeverity.Low, SonarQubeSoftwareQualitySeverity.Info }, RuleAction.Info],
        [new[] { SonarQubeSoftwareQualitySeverity.Low, SonarQubeSoftwareQualitySeverity.Info, SonarQubeSoftwareQualitySeverity.High }, RuleAction.Warning],
    ];

    public static object[][] AllMqrSeverities =>
        Enum.GetValues(typeof(SonarQubeSoftwareQualitySeverity)).Cast<object>()
            .Select(severity => new[] { severity })
            .ToArray();

    public static object[][] AllSeverities =>
        Enum.GetValues(typeof(SonarQubeIssueSeverity)).Cast<object>()
            .Select(severity => new[] { severity })
            .ToArray();

    private static SonarQubeRule CreateMqrRule(params SonarQubeSoftwareQualitySeverity[] mqrSeverities) => CreateMqrRule(isActive: true, mqrSeverities);

    private static SonarQubeRule CreateMqrRule(bool isActive, params SonarQubeSoftwareQualitySeverity[] mqrSeverities)
    {
        mqrSeverities.Should().NotBeEmpty();
        var sonarQubeSoftwareQualitySeverities =
            Enum.GetValues(typeof(SonarQubeSoftwareQuality))
                .Cast<SonarQubeSoftwareQuality>()
                .Zip(mqrSeverities, (x, y) => (x, y))
                .ToDictionary(k => k.x, v => v.y);
        return CreateRule(sonarQubeSoftwareQualitySeverities, SonarQubeIssueSeverity.Blocker, isActive: isActive);
    }

    private static SonarQubeRule CreateStandardRule(SonarQubeIssueSeverity severity, bool isActive = true) => CreateRule(null, severity, isActive);

    private static SonarQubeRule CreateRule(Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> mqrSeverity, SonarQubeIssueSeverity severity, bool isActive = true) =>
        new(default,
            default,
            isActive,
            severity,
            default,
            mqrSeverity,
            default,
            default);
}
