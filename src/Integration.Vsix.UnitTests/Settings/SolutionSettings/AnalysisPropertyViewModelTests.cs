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
using SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings.SolutionSettings;

[TestClass]
public class AnalysisPropertyViewModelTests
{
    private AnalysisPropertyViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new AnalysisPropertyViewModel(null, null);

    [TestMethod]
    public void Name_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.Name = "**/*.cs";

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Name)));
    }

    [TestMethod]
    public void Value_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.Value = "**/*.cs";

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Value)));
    }

    [TestMethod]
    public void Error_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.Name = ",";

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.Error)));
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.HasError)));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("\t")]
    public void Name_EmptyString_HasError(string empty)
    {
        testSubject.Name = empty;
        testSubject.Value = "value";

        testSubject.Error.Should().Be(Strings.AddAnalysisPropertyDialog_EmptyErrorMessage);
        testSubject.HasError.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("\t")]
    public void Value_EmptyString_HasError(string empty)
    {
        testSubject.Value = empty;
        testSubject.Name = "prop";

        testSubject.Error.Should().Be(Strings.AddAnalysisPropertyDialog_EmptyErrorMessage);
        testSubject.HasError.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("\t")]
    public void NameAndValue_Empty_HasError(string empty)
    {
        testSubject.Name = empty;
        testSubject.Value = empty;

        testSubject.Error.Should().Be(Strings.AddAnalysisPropertyDialog_EmptyErrorMessage);
        testSubject.HasError.Should().BeTrue();
    }

    [TestMethod]
    public void NameAndValue_Filled_HasNoError()
    {
        testSubject.Name = "prop";
        testSubject.Value = "value";

        testSubject.Error.Should().BeNull();
        testSubject.HasError.Should().BeFalse();
    }
}
