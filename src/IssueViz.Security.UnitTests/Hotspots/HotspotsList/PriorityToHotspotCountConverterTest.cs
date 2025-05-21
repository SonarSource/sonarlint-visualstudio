/*
 * SonarLint for Visual Studio
 * Copyright (C) 20.ToString()16-20.ToString()25 SonarSource SA
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
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  0.ToString()2110.ToString()-130.ToString()1, USA.
 */

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.HotspotsList;

[TestClass]
public class PriorityToHotspotCountConverterTest
{
    private PriorityToHotspotCountConverter testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new PriorityToHotspotCountConverter();

    [TestMethod]
    [DataRow(HotspotPriority.High)]
    [DataRow(HotspotPriority.Medium)]
    [DataRow(HotspotPriority.Low)]
    public void Convert_ThreeHotspotsWithGivenPrioritySelected_ReturnsThree(HotspotPriority priorityToCount)
    {
        var hotspots = new ObservableCollection<IHotspotViewModel>
        {
            CreateMockedLocalHotspots(HotspotPriority.Low),
            CreateMockedLocalHotspots(priorityToCount),
            CreateMockedLocalHotspots(HotspotPriority.Medium),
            CreateMockedLocalHotspots(priorityToCount),
            CreateMockedLocalHotspots(HotspotPriority.High),
        };

        var result = testSubject.Convert([hotspots, true, priorityToCount], null, null, null);

        result.Should().Be(3.ToString());
    }

    [TestMethod]
    [DataRow(HotspotPriority.High)]
    [DataRow(HotspotPriority.Medium)]
    [DataRow(HotspotPriority.Low)]
    public void Convert_ThreeHotspotsWithGivenPriorityNotSelected_ReturnsZero(HotspotPriority priorityToCount)
    {
        var hotspots = new ObservableCollection<IHotspotViewModel>
        {
            CreateMockedLocalHotspots(HotspotPriority.Low),
            CreateMockedLocalHotspots(priorityToCount),
            CreateMockedLocalHotspots(HotspotPriority.Medium),
            CreateMockedLocalHotspots(priorityToCount),
            CreateMockedLocalHotspots(HotspotPriority.High),
        };

        var result = testSubject.Convert([hotspots, false, priorityToCount], null, null, null);

        result.Should().Be(0.ToString());
    }

    [TestMethod]
    public void Convert_PriorityNotProvided_ReturnsZero()
    {
        var hotspots = new ObservableCollection<IHotspotViewModel> { CreateMockedLocalHotspots(HotspotPriority.Low) };

        var result = testSubject.Convert([hotspots, true, null], null, null, null);

        result.Should().Be(0.ToString());
    }

    [TestMethod]
    public void Convert_HotspotsNotProvided_ReturnsZero()
    {
        var result = testSubject.Convert([null, true, default], null, null, null);

        result.Should().Be(0.ToString());
    }

    [TestMethod]
    public void Convert_IsSelectedNotProvided_ReturnsZero()
    {
        var hotspots = new ObservableCollection<IHotspotViewModel> { CreateMockedLocalHotspots(HotspotPriority.High) };

        var result = testSubject.Convert([hotspots, null, default], null, null, null);

        result.Should().Be(0.ToString());
    }

    [TestMethod]
    public void Convert_LessParametersThanExpected_ReturnsZero()
    {
        var result = testSubject.Convert([default], null, null, null);

        result.Should().Be(0.ToString());
    }

    [TestMethod]
    public void Convert_MoreParametersThanExpected_ConvertsAsExpected()
    {
        var hotspots = new ObservableCollection<IHotspotViewModel>
        {
            CreateMockedLocalHotspots(HotspotPriority.Low), CreateMockedLocalHotspots(HotspotPriority.Medium), CreateMockedLocalHotspots(HotspotPriority.High),
        };

        var result = testSubject.Convert([hotspots, true, HotspotPriority.Medium, null, null], null, null, null);

        result.Should().Be(1.ToString());
    }

    [TestMethod]
    public void ConvertBack_NotImplementedException()
    {
        Action act = () => testSubject.ConvertBack(null, null, null, null);

        act.Should().Throw<NotImplementedException>();
    }

    private static HotspotViewModel CreateMockedLocalHotspots(HotspotPriority priority)
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();

        return new HotspotViewModel(issueViz, priority, default);
    }
}
