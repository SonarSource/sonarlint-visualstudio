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
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Integration.Transition;

namespace SonarLint.VisualStudio.Integration.UnitTests.Transition;

[TestClass]
public class MuteViewModelTests
{
    private MuteViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new MuteViewModel();

    [TestMethod]
    public void Cto_InitializesProperties()
    {
        testSubject.Comment.Should().BeNull();
        testSubject.AllowedStatusViewModels.Should().BeEmpty();
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
        testSubject.SelectedStatusViewModel = new StatusViewModel(SonarQubeIssueTransition.Accept, "title", "description");

        testSubject.IsSubmitButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow("\n")]
    [DataRow("\t")]
    [DataRow(null)]
    public void IsSubmitButtonEnabled_CommentIsEmptyAndStatusIsSelected_ReturnsTrue(string comment)
    {
        testSubject.Comment = comment;
        testSubject.SelectedStatusViewModel = new StatusViewModel(SonarQubeIssueTransition.Accept, "title", "description");

        testSubject.IsSubmitButtonEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void Comment_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.Comment = "some comment";

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Comment)));
    }

    [TestMethod]
    public void SelectedStatusViewModel_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedStatusViewModel = new StatusViewModel(SonarQubeIssueTransition.Accept, "title", "description");

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedStatusViewModel)));
        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsSubmitButtonEnabled)));
    }

    [TestMethod]
    public void InitializeStatuses_InitializesAllowedStatusViewModels()
    {
        var transitions = new[] { SonarQubeIssueTransition.Accept, SonarQubeIssueTransition.WontFix };

        testSubject.InitializeStatuses(transitions);

        testSubject.AllowedStatusViewModels.Should().HaveCount(2);
        testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.Transition == SonarQubeIssueTransition.Accept);
        testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.Transition == SonarQubeIssueTransition.WontFix);
    }

    [TestMethod]
    public void InitializeStatuses_ClearsPreviousStatuses()
    {
        testSubject.InitializeStatuses([SonarQubeIssueTransition.Accept]);
        testSubject.InitializeStatuses([SonarQubeIssueTransition.WontFix]);

        testSubject.AllowedStatusViewModels.Should().HaveCount(1);
        testSubject.AllowedStatusViewModels.Should().ContainSingle(x => x.Transition == SonarQubeIssueTransition.WontFix);
    }

    [TestMethod]
    public void InitializeStatuses_ClearsSelectedStatusViewModel()
    {
        var transitions = new[] { SonarQubeIssueTransition.Accept, SonarQubeIssueTransition.WontFix };
        testSubject.SelectedStatusViewModel = new StatusViewModel(SonarQubeIssueTransition.Accept, "title", "description");

        testSubject.InitializeStatuses(transitions);

        testSubject.SelectedStatusViewModel.Should().BeNull();
    }
}
