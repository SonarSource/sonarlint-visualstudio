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

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class DependencyRiskImpactSeverityToImageSourceConverterTest
{
    private DependencyRiskImpactSeverityToImageSourceConverter testSubject;
    private IResourceFinder resourceFinder;
    private Button uiElement;

    [TestInitialize]
    public void Initialize()
    {
        uiElement = new Button();
        resourceFinder = Substitute.For<IResourceFinder>();
        testSubject = new DependencyRiskImpactSeverityToImageSourceConverter();
    }

    [TestMethod]
    public void Convert_ReturnsNull_WhenInvalidNumberOfParametersUsed()
    {
        var result = testSubject.Convert([default, default, default, default], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_ReturnsNull_WhenDependencyRiskImpactSeverityNotProvided()
    {
        var result = testSubject.Convert([null, uiElement, resourceFinder], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_ReturnsNull_WhenUiElementNotProvided()
    {
        var result = testSubject.Convert([DependencyRiskImpactSeverity.High, null, resourceFinder], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_ReturnsNull_WhenResourceFinderNotProvided()
    {
        var result = testSubject.Convert([DependencyRiskImpactSeverity.High, uiElement, null], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    [DynamicData(nameof(GetAllDependencyRiskImpactSeverities))]
    public void Convert_ReturnsResourceForSeverity(DependencyRiskImpactSeverity severity)
    {
        var expectedResource = new Style();
        resourceFinder.TryFindResource(uiElement, $"{severity}SeverityDrawingImage").Returns(expectedResource);

        var result = testSubject.Convert([severity, uiElement, resourceFinder], null, "Severity", CultureInfo.InvariantCulture);

        resourceFinder.Received(1).TryFindResource(uiElement, $"{severity}SeverityDrawingImage");
        result.Should().Be(expectedResource);
    }

    [TestMethod]
    public void Convert_ParameterNotProvided_SearchesForResource()
    {
        testSubject.Convert([DependencyRiskImpactSeverity.Blocker, uiElement, resourceFinder], null, null, CultureInfo.InvariantCulture);

        resourceFinder.Received(1).TryFindResource(uiElement, $"{DependencyRiskImpactSeverity.Blocker}DrawingImage");
    }

    [TestMethod]
    public void ConvertBack_ThrowsException()
    {
        var act = () => testSubject.ConvertBack(null, null, null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }

    public static object[][] GetAllDependencyRiskImpactSeverities =>
        Enum.GetValues(typeof(DependencyRiskImpactSeverity)).Cast<object>()
            .Select(severity => new[] { severity })
            .ToArray();
}
