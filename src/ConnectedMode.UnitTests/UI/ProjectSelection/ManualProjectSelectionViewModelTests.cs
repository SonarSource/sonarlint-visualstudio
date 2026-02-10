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
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ProjectSelection;

[TestClass]
public class ManualProjectSelectionViewModelTests
{
    private ManualProjectSelectionViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new ManualProjectSelectionViewModel();

    [TestMethod]
    public void ProjectKey_NotSet_DefaultsToNull()
    {
        testSubject.ProjectKey.Should().BeNull();
        testSubject.IsValidProjectKey.Should().BeFalse();
    }

    [TestMethod]
    public void ProjectKey_Set_EventsRaised()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.ProjectKey = "key";

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.ProjectKey)));
        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsValidProjectKey)));
    }

    [TestMethod]
    public void ProjectKey_Set_PreventsSpaces()
    {
        testSubject.ProjectKey = " k e y ";

        testSubject.ProjectKey.Should().Be("key");
    }

    [TestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("       ", false)]
    [DataRow("my key", true)]
    [DataRow("key", true)]
    public void IsValidProjectKey_Validates(string key, bool expectedResult)
    {
        testSubject.ProjectKey = key;

        testSubject.IsValidProjectKey.Should().Be(expectedResult);
    }
}
