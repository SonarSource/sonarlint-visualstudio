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
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.Settings.FileExclusions;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings.FileExclusions;

[TestClass]
public class ExclusionViewModelTest
{
    [TestMethod]
    public void Pattern_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        var testSubject = new ExclusionViewModel(string.Empty);
        testSubject.PropertyChanged += eventHandler;

        testSubject.Pattern = "**/*.cs";

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Pattern)));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("\t")]
    public void Pattern_EmptyString_ReturnsErrorMessage(string exclusion)
    {
        var testSubject = new ExclusionViewModel(exclusion);

        var error = testSubject[nameof(testSubject.Pattern)];

        error.Should().Be(Strings.FileExclusions_PatternErrorMessage);
        testSubject.Error.Should().Be(error);
    }

    [TestMethod]
    public void Pattern_NonEmptyStrings_HasNoError()
    {
        var testSubject = new ExclusionViewModel("**/*.cs");

        var error = testSubject[nameof(testSubject.Pattern)];

        error.Should().BeNull();
        testSubject.Error.Should().BeNull();
    }
}
