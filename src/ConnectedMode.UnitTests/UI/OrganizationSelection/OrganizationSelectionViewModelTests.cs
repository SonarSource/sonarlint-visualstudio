﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.OrganizationSelection;

[TestClass]
public class OrganizationSelectionViewModelTests
{
    private OrganizationSelectionViewModel testSubject;
    private ISlCoreConnectionAdapter slCoreConnectionAdapter;
    private ICredentialsModel credentialsModel;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        credentialsModel = Substitute.For<ICredentialsModel>();
        testSubject = new(credentialsModel, slCoreConnectionAdapter);
    }

    [TestMethod]
    public void Ctor_Organizations_SetsEmptyAsDefault()
    {
        new OrganizationSelectionViewModel(credentialsModel, slCoreConnectionAdapter).Organizations.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_OrganizationList_SetsPropertyValue()
    { 
        var organization = new OrganizationDisplay("key", "name");

        testSubject.AddOrganization(organization);

        testSubject.Organizations.Count.Should().Be(1);
        testSubject.Organizations[0].Should().Be(organization);
    }

    [TestMethod]
    public void SelectedOrganization_NotSet_ValueIsNull()
    {
        testSubject.SelectedOrganization.Should().BeNull();
        testSubject.IsValidSelectedOrganization.Should().BeFalse();
    }

    [TestMethod]
    public void SelectedOrganization_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedOrganization = new OrganizationDisplay("key", "name");

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedOrganization)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsValidSelectedOrganization)));
    }

    [TestMethod]
    public void IsValidSelectedOrganization_NullOrganization_ReturnsFalse()
    {
        testSubject.SelectedOrganization = null;

        testSubject.IsValidSelectedOrganization.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("      ")]
    public void IsValidSelectedOrganization_OrganizationWithInvalidKey_ReturnsFalse(string key)
    {
        testSubject.SelectedOrganization = new OrganizationDisplay(key, "value");

        testSubject.IsValidSelectedOrganization.Should().BeFalse();
    }
    
    [DataTestMethod]
    [DataRow("mykey")]
    [DataRow("my key")]
    public void IsValidSelectedOrganization_OrganizationWithValidKey_ReturnsTrue(string key)
    {
        testSubject.SelectedOrganization = new OrganizationDisplay(key, "value");

        testSubject.IsValidSelectedOrganization.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow("key", null, true)]
    [DataRow("key", "name", true)]
    [DataRow(null, null, false)]
    [DataRow(null, "name", false)]
    public void IsValidSelectedOrganization_IgnoresName(string key, string name, bool expectedResult)
    {
        testSubject.SelectedOrganization = new OrganizationDisplay(key, name);

        testSubject.IsValidSelectedOrganization.Should().Be(expectedResult);
    }

    [TestMethod]
    public void NoOrganizationExists_NoOrganizations_ReturnsTrue()
    { 
        testSubject.NoOrganizationExists.Should().BeTrue();
    }

    [TestMethod]
    public void NoOrganizationExists_HasOrganizations_ReturnsFalse()
    {
        testSubject.AddOrganization(new OrganizationDisplay("key", "name"));

        testSubject.NoOrganizationExists.Should().BeFalse();
    }
}
