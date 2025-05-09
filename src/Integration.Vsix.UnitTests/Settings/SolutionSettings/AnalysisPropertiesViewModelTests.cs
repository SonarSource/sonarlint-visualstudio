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

using System.Collections.Immutable;
using System.ComponentModel;
using NuGet;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.Vsix.Settings.SolutionSettings;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings.SolutionSettings;

[TestClass]
public class AnalysisPropertiesViewModelTests
{
    private static readonly AnalysisPropertyViewModel PropertyViewModel = new("prop1", "value1");
    private IUserSettingsProvider userSettingsProvider;
    private AnalysisPropertiesViewModel testSubject;

    [TestInitialize]
    public void Initialize()
    {
        userSettingsProvider = Substitute.For<IUserSettingsProvider>();
        testSubject = new AnalysisPropertiesViewModel(userSettingsProvider);
    }

    [TestMethod]
    public void InitializeAnalysisProperties_EmptyProperties_DoesNotAddAnyProperties()
    {
        var userSettings = new UserSettings(new AnalysisSettings(), "aBaseDir");
        userSettingsProvider.UserSettings.Returns(userSettings);

        testSubject.InitializeAnalysisProperties();

        testSubject.AnalysisProperties.Should().BeEmpty();
        userSettingsProvider.Received(1).EnsureSolutionAnalysisSettingsFileExists();
    }

    [TestMethod]
    public void InitializeAnalysisProperties_WithProperties_AddsAllProperties()
    {
        var properties = new Dictionary<string, string> { { "prop1", "value1" }, { "prop2", "value2" } };
        var userSettings = new UserSettings(new AnalysisSettings(analysisProperties: properties.ToImmutableDictionary()), "aBaseDir");
        userSettingsProvider.UserSettings.Returns(userSettings);

        testSubject.InitializeAnalysisProperties();

        testSubject.AnalysisProperties.Should().HaveCount(2);
        testSubject.AnalysisProperties.Should().Contain(x => x.Name == "prop1" && x.Value == "value1");
        testSubject.AnalysisProperties.Should().Contain(x => x.Name == "prop2" && x.Value == "value2");
        userSettingsProvider.Received(1).EnsureSolutionAnalysisSettingsFileExists();
    }

    [TestMethod]
    public void InitializeAnalysisProperties_WithProperties_SetsSelectedProperty()
    {
        var properties = new Dictionary<string, string> { { "prop1", "value1" } };
        var userSettings = new UserSettings(new AnalysisSettings(analysisProperties: properties.ToImmutableDictionary()), "aBaseDir");
        userSettingsProvider.UserSettings.Returns(userSettings);

        testSubject.InitializeAnalysisProperties();

        testSubject.SelectedProperty.Should().NotBeNull();
        testSubject.SelectedProperty.Name.Should().Be("prop1");
        testSubject.SelectedProperty.Value.Should().Be("value1");
    }

    [TestMethod]
    public void InitializeAnalysisProperties_WithProperties_ClearsExistingProperties()
    {
        // Arrange
        testSubject.AnalysisProperties.Add(new AnalysisPropertyViewModel("existing", "value"));
        var properties = new Dictionary<string, string> { { "prop1", "value1" } };
        var userSettings = new UserSettings(new AnalysisSettings(analysisProperties: properties.ToImmutableDictionary()), "aBaseDir");
        userSettingsProvider.UserSettings.Returns(userSettings);

        // Act
        testSubject.InitializeAnalysisProperties();

        // Assert
        testSubject.AnalysisProperties.Should().HaveCount(1);
        testSubject.AnalysisProperties.Should().Contain(x => x.Name == "prop1" && x.Value == "value1");
    }

    [TestMethod]
    public void SelectedProperty_Setter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedProperty = PropertyViewModel;

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedProperty)));
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsAnyPropertySelected)));
    }

    [TestMethod]
    public void IsAnyPropertySelected_SelectedPropertyNotNull_ReturnsTrue()
    {
        testSubject.SelectedProperty = PropertyViewModel;

        testSubject.IsAnyPropertySelected.Should().BeTrue();
    }

    [TestMethod]
    public void IsAnyPropertySelected_SelectedPropertyNull_ReturnsFalse()
    {
        testSubject.SelectedProperty = null;

        testSubject.IsAnyPropertySelected.Should().BeFalse();
    }

    [TestMethod]
    public void UpdateAnalysisProperties_NoProperties_UpdatesSolutionAnalysisProperties()
    {
        testSubject.UpdateAnalysisProperties();

        userSettingsProvider.Received(1).UpdateAnalysisProperties(Arg.Is<Dictionary<string, string>>(x => x.IsEmpty()));
    }

    [TestMethod]
    public void UpdateAnalysisProperties_ValidProperties_UpdatesSolutionAnalysisProperties()
    {
        testSubject.AnalysisProperties.Add(new AnalysisPropertyViewModel("prop1", "value1"));
        testSubject.AnalysisProperties.Add(new AnalysisPropertyViewModel("prop2", "value2"));

        testSubject.UpdateAnalysisProperties();

        userSettingsProvider.Received(1).UpdateAnalysisProperties(Arg.Is<Dictionary<string, string>>(x =>
            x.Count == 2
            && x["prop1"] == "value1"
            && x["prop2"] == "value2"));
    }

    [TestMethod]
    public void AddProperty_AddsNewProperty()
    {
        var name = "prop";
        var value = "value";

        testSubject.AddProperty(name, value);

        testSubject.AnalysisProperties.Should().HaveCount(1);
        testSubject.SelectedProperty.Should().NotBeNull();
        testSubject.SelectedProperty.Name.Should().Be(name);
        testSubject.SelectedProperty.Value.Should().Be(value);
    }

    [TestMethod]
    public void AddProperty_DuplicateProperty_OverwritesExistingProperty()
    {
        testSubject.AddProperty("prop", "value");
        testSubject.AnalysisProperties.Should().HaveCount(1);
        var selectedProperty = testSubject.SelectedProperty;
        testSubject.SelectedProperty.Name.Should().Be("prop");
        testSubject.SelectedProperty.Value.Should().Be("value");

        testSubject.AddProperty("prop", "value2");
        testSubject.AnalysisProperties.Should().HaveCount(1);
        testSubject.SelectedProperty.Should().BeSameAs(selectedProperty);
        testSubject.SelectedProperty.Value.Should().Be("value2");
    }

    [TestMethod]
    public void RemoveSelectedProperty_SelectedPropertyNotNull_RemovesExclusion()
    {
        testSubject.AnalysisProperties.Add(PropertyViewModel);
        testSubject.SelectedProperty = PropertyViewModel;

        testSubject.RemoveSelectedProperty();

        testSubject.AnalysisProperties.Should().BeEmpty();
        testSubject.SelectedProperty.Should().BeNull();
    }

    [TestMethod]
    public void RemoveSelectedProperty_SelectedPropertyNull_DoesNotRemoveAnyProperty()
    {
        testSubject.AnalysisProperties.Add(PropertyViewModel);
        testSubject.SelectedProperty = null;

        testSubject.RemoveSelectedProperty();

        testSubject.AnalysisProperties.Should().HaveCount(1);
        testSubject.SelectedProperty.Should().BeNull();
    }
}
