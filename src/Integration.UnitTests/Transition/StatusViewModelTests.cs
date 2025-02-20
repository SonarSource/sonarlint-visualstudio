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
using SonarLint.VisualStudio.Integration.Transition;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Transition;

[TestClass]
public class StatusViewModelTests
{
    [TestMethod]
    [DataRow(SonarQubeIssueTransition.FalsePositive, "false positive", "description1")]
    [DataRow(SonarQubeIssueTransition.WontFix, "won't fix", "description2")]
    [DataRow(SonarQubeIssueTransition.Accept, "accept", "description\ndescription2")]
    public void Ctor_InitializesProperties(SonarQubeIssueTransition transition, string title, string description)
    {
        var testSubject = new StatusViewModel(transition, title, description);

        testSubject.Transition.Should().Be(transition);
        testSubject.Title.Should().Be(title);
        testSubject.Description.Should().Be(description);
        testSubject.IsChecked.Should().BeFalse();
    }

    [TestMethod]
    public void IsChecked_Set_RaisesEvents()
    {
        var testSubject = new StatusViewModel(SonarQubeIssueTransition.Accept, "title", "description");
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.IsChecked = !testSubject.IsChecked;

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsChecked)));
    }
}
