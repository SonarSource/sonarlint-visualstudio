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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class DependencyRiskTypeToLocalizedTypeConverterTest
{
    private DependencyRiskTypeToLocalizedTypeConverter testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new DependencyRiskTypeToLocalizedTypeConverter();

    [DataTestMethod]
    [DynamicData(nameof(GetDependencyRiskTypesToLocalization), DynamicDataSourceType.Method)]
    public void Convert_typeProvided_ConvertsAsExpected(DependencyRiskType type, string expected)
    {
        var result = testSubject.Convert(type, null, null, null);

        result.Should().Be(expected);
    }

    [TestMethod]
    public void Convert_InvalidDependencyRiskType_ReturnsEmptyString()
    {
        var result = testSubject.Convert((DependencyRiskType)13, null, null, null);

        result.Should().Be(string.Empty);
    }

    [TestMethod]
    public void Convert_NoDependencyRiskTypeProvided_ReturnsEmptyString()
    {
        var result = testSubject.Convert(null, null, null, null);

        result.Should().Be(string.Empty);
    }

    [TestMethod]
    public void ConvertBack_NotImplementedException()
    {
        Action act = () => testSubject.ConvertBack(null, null, null, null);

        act.Should().Throw<NotImplementedException>();
    }

    public static IEnumerable<object[]> GetDependencyRiskTypesToLocalization() =>
    [
        [DependencyRiskType.Vulnerability, DependencyRiskType.Vulnerability.ToString()],
        [DependencyRiskType.ProhibitedLicense, Resources.DependencyRiskType_ProhibitedLicense],
    ];
}
