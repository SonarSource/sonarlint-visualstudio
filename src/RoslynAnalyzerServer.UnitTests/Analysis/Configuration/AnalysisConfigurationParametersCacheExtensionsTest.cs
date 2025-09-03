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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class AnalysisConfigurationParametersCacheExtensionsTest
{
    private readonly AnalyzerInfoDto defaultAnalyzerInfoDto = default;
    private readonly KeyValuePair<string, string> disableRazorAnalysisProp = new("sonar.cs.internal.disableRazor", "true");
    private readonly ActiveRuleDto s101RuleWithParam = new("S101", new Dictionary<string, string> { { "threshold", "3" } });

    [TestMethod]
    public void ShouldInvalidateCache_CacheIsNull_ReturnsTrue()
    {
        AnalysisConfigurationParametersCache? testSubject = null;

        var result = testSubject.ShouldInvalidateCache([], [], defaultAnalyzerInfoDto);

        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void ShouldInvalidateCache_AnalyzerInfoDtoHasDifferentValues_ReturnsTrue(bool useSharpEnterprise, bool useVbNetEnterprise)
    {
        var testSubject = new AnalysisConfigurationParametersCache([], [], defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache([], [], new(useSharpEnterprise, useVbNetEnterprise));

        result.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [DataRow(false, false)]
    public void ShouldInvalidateCache_AnalyzerInfoDtoHasSameValues_ReturnsFalse(bool useSharpEnterprise, bool useVbNetEnterprise)
    {
        var testSubject = new AnalysisConfigurationParametersCache([], [], new(useSharpEnterprise, useVbNetEnterprise));

        var result = testSubject.ShouldInvalidateCache([], [], new(useSharpEnterprise, useVbNetEnterprise));

        result.Should().BeFalse();
    }

    [TestMethod]
    public void ShouldInvalidateCache_SameActiveRules_ReturnsFalse()
    {
        var activeRules = new Dictionary<string, ActiveRuleDto> { { s101RuleWithParam.RuleId, s101RuleWithParam } };
        var sameActiveRules = new List<ActiveRuleDto> { s101RuleWithParam with { Parameters = new Dictionary<string, string>(s101RuleWithParam.Parameters) } };
        var testSubject = new AnalysisConfigurationParametersCache(activeRules, [], defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache(sameActiveRules, [], defaultAnalyzerInfoDto);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void ShouldInvalidateCache_DifferentActiveRules_ReturnsTrue()
    {
        var activeRules = new Dictionary<string, ActiveRuleDto> { { s101RuleWithParam.RuleId, s101RuleWithParam } };
        var newActiveRules = new List<ActiveRuleDto> { new("S102", new Dictionary<string, string> { { "timeout", "60" } }) };
        var testSubject = new AnalysisConfigurationParametersCache(activeRules, [], defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache(newActiveRules, [], defaultAnalyzerInfoDto);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldInvalidateCache_SameRuleWithDifferentParameter_ReturnsTrue()
    {
        var activeRules = new Dictionary<string, ActiveRuleDto> { { s101RuleWithParam.RuleId, s101RuleWithParam } };
        var newActiveRules = new List<ActiveRuleDto> { s101RuleWithParam with { Parameters = new Dictionary<string, string> { { "timeout", "60" } } } };
        var testSubject = new AnalysisConfigurationParametersCache(activeRules, [], defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache(newActiveRules, [], defaultAnalyzerInfoDto);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldInvalidateCache_SameRuleWithDifferentParameters_ReturnsTrue()
    {
        var activeRules = new Dictionary<string, ActiveRuleDto> { { s101RuleWithParam.RuleId, s101RuleWithParam } };
        var newActiveRules = new List<ActiveRuleDto> { s101RuleWithParam with { Parameters = [] } };
        var testSubject = new AnalysisConfigurationParametersCache(activeRules, [], defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache(newActiveRules, [], defaultAnalyzerInfoDto);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldInvalidateCache_SameRuleWithDifferentParameterValue_ReturnsTrue()
    {
        var activeRules = new Dictionary<string, ActiveRuleDto> { { s101RuleWithParam.RuleId, s101RuleWithParam } };
        var newActiveRules = new List<ActiveRuleDto> { s101RuleWithParam with { Parameters = new Dictionary<string, string> { { "threshold", "5" } } } };
        var testSubject = new AnalysisConfigurationParametersCache(activeRules, [], defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache(newActiveRules, [], defaultAnalyzerInfoDto);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldInvalidateCache_SameAnalysisProperties_ReturnsFalse()
    {
        var analysisProperties = new Dictionary<string, string> { { disableRazorAnalysisProp.Key, disableRazorAnalysisProp.Value } };
        var sameAnalysisProperties = new Dictionary<string, string> { { disableRazorAnalysisProp.Key, disableRazorAnalysisProp.Value } };
        var testSubject = new AnalysisConfigurationParametersCache([], analysisProperties, defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache([], sameAnalysisProperties, defaultAnalyzerInfoDto);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void ShouldInvalidateCache_SameAnalysisPropertiesWithDifferentValue_ReturnsTrue()
    {
        var analysisProperties = new Dictionary<string, string> { { disableRazorAnalysisProp.Key, disableRazorAnalysisProp.Value } };
        var newAnalysisProperties = new Dictionary<string, string> { { disableRazorAnalysisProp.Key, "false" } };
        var testSubject = new AnalysisConfigurationParametersCache([], analysisProperties, defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache([], newAnalysisProperties, defaultAnalyzerInfoDto);

        result.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldInvalidateCache_DifferentAnalysisProperties_ReturnsTrue()
    {
        var analysisProperties = new Dictionary<string, string> { { disableRazorAnalysisProp.Key, disableRazorAnalysisProp.Value } };
        var newAnalysisProperties = new Dictionary<string, string>();
        var testSubject = new AnalysisConfigurationParametersCache([], analysisProperties, defaultAnalyzerInfoDto);

        var result = testSubject.ShouldInvalidateCache([], newAnalysisProperties, defaultAnalyzerInfoDto);

        result.Should().BeTrue();
    }
}
