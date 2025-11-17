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
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots;

[TestClass]
public class ChangeHotspotStatusViewModelTest
{
    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        var currentStatus = HotspotStatus.Fixed;
        List<HotspotStatus> allowedStatuses = [HotspotStatus.Fixed, HotspotStatus.Acknowledged];

        var testSubject = new ChangeHotspotStatusViewModel(currentStatus, allowedStatuses);

        testSubject.AllStatusViewModels.Should().HaveCount(allowedStatuses.Count);
        testSubject.AllStatusViewModels.Should().Contain(x => x.GetCurrentStatus<HotspotStatus>() == HotspotStatus.Fixed);
        testSubject.AllStatusViewModels.Should().Contain(x => x.GetCurrentStatus<HotspotStatus>() == HotspotStatus.Acknowledged);
        testSubject.SelectedStatusViewModel.GetCurrentStatus<HotspotStatus>().Should().Be(currentStatus);
        testSubject.ShowComment.Should().BeFalse();
    }

    [TestMethod]
    public void Ctor_NoStatusHasMandatoryComment()
    {
        var allowedStatuses = Enum.GetValues(typeof(HotspotStatus)).Cast<HotspotStatus>().ToList();

        var testSubject = new ChangeHotspotStatusViewModel(default, allowedStatuses);

        testSubject.AllStatusViewModels.Should().HaveCount(allowedStatuses.Count);
        testSubject.AllStatusViewModels.All(vm => !vm.IsCommentRequired).Should().BeTrue();
    }

    [TestMethod]
    public void SelectedStatus_IsInListOfAllowedStatuses_InitializesCorrectly()
    {
        var currentStatus = HotspotStatus.Acknowledged;
        List<HotspotStatus> allowedStatuses = [HotspotStatus.Fixed, HotspotStatus.Acknowledged];

        var testSubject = new ChangeHotspotStatusViewModel(currentStatus, allowedStatuses);

        testSubject.SelectedStatusViewModel.GetCurrentStatus<HotspotStatus>().Should().Be(currentStatus);
    }

    [TestMethod]
    public void SelectedStatus_IsNotInListOfAllowedStatuses_InitializesNull()
    {
        var currentStatus = HotspotStatus.Safe;
        List<HotspotStatus> allowedStatuses = [HotspotStatus.Fixed, HotspotStatus.Acknowledged];

        var testSubject = new ChangeHotspotStatusViewModel(currentStatus, allowedStatuses);

        testSubject.SelectedStatusViewModel.Should().BeNull();
    }
}
