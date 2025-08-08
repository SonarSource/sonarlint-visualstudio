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

using System.ComponentModel;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReviewStatus;

[TestClass]
public class StatusViewModelTest
{
    private const string StatusTitle = "title";
    private const string StatusDescription = "description";

    [TestMethod]
    [DataRow(HotspotStatus.ToReview, "to review", "description1", false)]
    [DataRow(HotspotStatus.Acknowledged, "acknowledges", "description\ndescription2", true)]
    [DataRow(HotspotStatus.Fixed, "fixed", "description3", false)]
    [DataRow(HotspotStatus.Safe, "safe", "description\n\tdescription4", true)]
    public void Ctor_InitializesProperties(
        HotspotStatus status,
        string title,
        string description,
        bool isCommentRequired)
    {
        var testSubject = new StatusViewModel<HotspotStatus>(status, title, description, isCommentRequired);

        testSubject.Status.Should().Be(status);
        testSubject.Title.Should().Be(title);
        testSubject.Description.Should().Be(description);
        testSubject.IsChecked.Should().BeFalse();
        testSubject.IsCommentRequired.Should().Be(isCommentRequired);
    }

    [TestMethod]
    public void IsChecked_Set_RaisesEvents()
    {
        var testSubject = CreateStatusViewModel(default);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.IsChecked = !testSubject.IsChecked;

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsChecked)));
    }

    [TestMethod]
    public void GetCurrentStatus_ReturnsCurrentStatus_WhenTypeIsCorrect()
    {
        var testSubject = CreateStatusViewModel(HotspotStatus.Fixed);

        var result = testSubject.GetCurrentStatus<HotspotStatus>();

        result.Should().Be(HotspotStatus.Fixed);
    }

    [TestMethod]
    public void GetCurrentStatus_Throws_WhenTypeIsIncorrect()
    {
        var testSubject = CreateStatusViewModel(HotspotStatus.Fixed);

        Action action = () => testSubject.GetCurrentStatus<DependencyRiskType>();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot get status of type {typeof(DependencyRiskType)} from {nameof(IStatusViewModel)} of type {typeof(HotspotStatus)}.");
    }

    private static StatusViewModel<HotspotStatus> CreateStatusViewModel(HotspotStatus status) => new(status, StatusTitle, StatusDescription, isCommentRequired: false);
}
