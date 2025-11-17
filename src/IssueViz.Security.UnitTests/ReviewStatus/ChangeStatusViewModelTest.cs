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

using System.ComponentModel;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReviewStatus;

[TestClass]
public class ChangeStatusViewModelTest
{
    private const string StatusTitle = "title";
    private const string StatusDescription = "description";
    private ChangeStatusViewModel<HotspotStatus> testSubject;
    private List<StatusViewModel<HotspotStatus>> allStatusViewModels;

    [TestInitialize]
    public void TestInitialize()
    {
        allStatusViewModels = Enum.GetValues(typeof(HotspotStatus))
            .Cast<HotspotStatus>()
            .Select(status => new StatusViewModel<HotspotStatus>(status, status.ToString(), status.ToString(), isCommentRequired: false)).ToList();

        testSubject = CreateTestSubject(HotspotStatus.Safe);
    }

    [TestMethod]
    [DataRow(HotspotStatus.Acknowledged, true)]
    [DataRow(HotspotStatus.Safe, false)]
    public void Ctor_InitializesProperties(HotspotStatus currentStatus, bool showComment)
    {
        testSubject = CreateTestSubject(currentStatus, showComment);

        testSubject.AllStatusViewModels.Should().HaveCount(allStatusViewModels.Count);
        testSubject.SelectedStatusViewModel.GetCurrentStatus<HotspotStatus>().Should().Be(currentStatus);
        testSubject.ShowComment.Should().Be(showComment);
    }

    [TestMethod]
    public void IsSubmitButtonEnabled_SelectedStatusViewModelIsNull_ReturnsFalse()
    {
        testSubject.SelectedStatusViewModel = null;

        testSubject.IsSubmitButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void IsSubmitButtonEnabled_SelectedStatusViewModelIsSet_NoValidationErrors_ReturnsTrue()
    {
        testSubject.SelectedStatusViewModel = CreateStatusViewModel(default);

        testSubject.IsSubmitButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void IsSubmitButtonEnabled_SelectedStatusViewModelIsSet_HasValidationErrors_ReturnsFalse()
    {
        testSubject.Comment = null;

        testSubject.SelectedStatusViewModel = CreateStatusViewModel(HotspotStatus.Safe, isCommentRequired: true);

        GetCommentValidationError().Should().NotBeNull();
        testSubject.IsSubmitButtonEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void SelectedStatusViewModel_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedStatusViewModel = CreateStatusViewModel(default);

        Received.InOrder(() =>
        {
            eventHandler.Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedStatusViewModel)));
            eventHandler.Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Comment)));
            eventHandler.Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSubmitButtonEnabled)));
        });
    }

    [TestMethod]
    public void Comment_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.Comment = "test";

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Comment)));
    }

    [TestMethod]
    public void Error_ReturnsCommentRequired_WhenSelectedStatusIsRequired()
    {
        testSubject.SelectedStatusViewModel = CreateStatusViewModel(HotspotStatus.Safe, isCommentRequired: true);

        testSubject.Comment = string.Empty;

        GetCommentValidationError().Should().Be(Resources.CommentRequiredErrorMessage);
        testSubject.Error.Should().Be(Resources.CommentRequiredErrorMessage);
    }

    [TestMethod]
    public void Error_ReturnsNull_WhenSelectedStatusIsNotRequired()
    {
        testSubject.SelectedStatusViewModel = CreateStatusViewModel(HotspotStatus.Fixed, isCommentRequired: false);

        testSubject.Comment = string.Empty;

        GetCommentValidationError().Should().BeNull();
        testSubject.Error.Should().BeNull();
    }

    [TestMethod]
    public void Error_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        GetCommentValidationError();

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSubmitButtonEnabled)));
    }

    private ChangeStatusViewModel<HotspotStatus> CreateTestSubject(HotspotStatus status, bool showComment = false) => new(status, allStatusViewModels, showComment);

    private string GetCommentValidationError() => testSubject[nameof(testSubject.Comment)];

    private static StatusViewModel<HotspotStatus> CreateStatusViewModel(HotspotStatus status, bool isCommentRequired = false) => new(status, StatusTitle, StatusDescription, isCommentRequired);
}
