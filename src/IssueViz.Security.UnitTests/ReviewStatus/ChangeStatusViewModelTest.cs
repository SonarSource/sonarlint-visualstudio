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
public class ChangeStatusViewModelTest
{
    private ChangeStatusViewModel<HotspotStatus> testSubject;
    private HotspotStatus[] allowedStatuses;
    private readonly HotspotStatus currentStatus = HotspotStatus.Safe;
    private List<StatusViewModel<HotspotStatus>> allStatusViewModels;

    [TestInitialize]
    public void TestInitialize()
    {
        allowedStatuses = [HotspotStatus.Acknowledged, HotspotStatus.Safe];
        allStatusViewModels = Enum.GetValues(typeof(HotspotStatus))
            .Cast<HotspotStatus>()
            .Select(status => new StatusViewModel<HotspotStatus>(status, status.ToString(), status.ToString())).ToList();

        testSubject = new ChangeStatusViewModel<HotspotStatus>(currentStatus, allowedStatuses, allStatusViewModels);
    }

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        testSubject.AllowedStatusViewModels.Should().HaveCount(allowedStatuses.Length);
        foreach (var allowedStatus in allowedStatuses)
        {
            testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.HasStatus(allowedStatus));
        }

        testSubject.SelectedStatusViewModel.HasStatus(currentStatus).Should().BeTrue();
        testSubject.SelectedStatusViewModel.IsChecked.Should().BeTrue();
    }

    [TestMethod]
    public void Ctor_CurrentStatusNotInListOfAllowedStatuses_SetsSelectionToNull()
    {
        testSubject = new ChangeStatusViewModel<HotspotStatus>(HotspotStatus.ToReview, allowedStatuses, allStatusViewModels);

        testSubject.SelectedStatusViewModel.Should().BeNull();
    }

    [TestMethod]
    public void IsSubmitButtonEnabled_SelectedStatusViewModelIsNull_ReturnsFalse()
    {
        testSubject.SelectedStatusViewModel = null;

        testSubject.IsSubmitButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsSubmitButtonEnabled_SelectedStatusViewModelIsSet_ReturnsTrue()
    {
        testSubject.SelectedStatusViewModel = new StatusViewModel<HotspotStatus>(default, "title", "description");

        testSubject.IsSubmitButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void SelectedStatusViewModel_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedStatusViewModel = new StatusViewModel<HotspotStatus>(default, "title", "description");

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedStatusViewModel)));
        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSubmitButtonEnabled)));
    }
}
