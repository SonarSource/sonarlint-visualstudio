﻿/*
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
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.ConnectionDisplay;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ConnectionDisplay;

[TestClass]
public class ConnectionNameViewModelTests
{
    private const string Id = "any id";
    public static object[][] Regions => [[CloudServerRegion.Eu], [CloudServerRegion.Us]];

    private ConnectionNameViewModel testSubject;
    private IConnectedModeUIServices connectedModeUIServices;
    private ISonarLintSettings sonarLintSettings;

    [TestInitialize]
    public void TestInitialize()
    {
        sonarLintSettings = Substitute.For<ISonarLintSettings>();
        connectedModeUIServices = Substitute.For<IConnectedModeUIServices>();
        testSubject = new ConnectionNameViewModel();

        MockConnectedModeUiServices();
    }

    [TestMethod]
    public void ConnectionInfo_Set_RaisesPropertyChanged()
    {
        var connectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarQube);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.ConnectionInfo = connectionInfo;

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == "ConnectionInfo"));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == "DisplayName"));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == "ShouldDisplayRegion"));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == "DisplayRegion"));
        testSubject.ConnectionInfo.Should().BeSameAs(connectionInfo);
    }

    [TestMethod]
    public void DisplayName_NoConnectionInfo_ReturnsEmpty()
    {
        testSubject.ConnectionInfo = null;

        testSubject.DisplayName.Should().BeEmpty();
    }

    [TestMethod]
    public void DisplayName_SonarQube_IdNull_ReturnsEmpty()
    {
        testSubject.ConnectionInfo = new ConnectionInfo(null, ConnectionServerType.SonarQube);

        testSubject.DisplayName.Should().BeEmpty();
    }

    [TestMethod]
    public void DisplayName_SonarQube_ReturnsId()
    {
        testSubject.ConnectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarQube);

        testSubject.DisplayName.Should().Be(Id);
    }

    [DynamicData(nameof(Regions))]
    [DataTestMethod]
    public void DisplayName_SonarCloud_NoId_HasRegion_ReturnsUrl(CloudServerRegion region)
    {
        testSubject.ConnectionInfo = new ConnectionInfo(null, ConnectionServerType.SonarCloud, region);

        testSubject.DisplayName.Should().Be(region.Url.ToString());
    }

    [TestMethod]
    public void DisplayName_SonarCloud_HasId_ReturnsId()
    {
        testSubject.ConnectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarCloud, CloudServerRegion.Eu);

        testSubject.DisplayName.Should().Be(Id);
    }

    [TestMethod]
    public void DisplayRegion_NoConnectionInfo_DoesNotDisplayRegion()
    {
        testSubject.ConnectionInfo = null;

        VerifyDoesNotDisplayRegion();
    }

    [TestMethod]
    public void DisplayRegion_SonarQube_DoesNotDisplayRegion()
    {
        testSubject.ConnectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarQube);

        VerifyDoesNotDisplayRegion();
    }

    [DynamicData(nameof(Regions))]
    [DataTestMethod]
    public void DisplayRegion_SonarCloud_NoId_DisplaysRegion(CloudServerRegion region)
    {
        testSubject.ConnectionInfo = new ConnectionInfo(null, ConnectionServerType.SonarCloud, region);

        testSubject.ShouldDisplayRegion.Should().BeTrue();
        testSubject.DisplayRegion.Should().Be(region.Name);
    }

    [DynamicData(nameof(Regions))]
    [DataTestMethod]
    public void DisplayRegion_SonarCloud_HasId_DisplaysRegion(CloudServerRegion region)
    {
        testSubject.ConnectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarCloud, region);

        testSubject.ShouldDisplayRegion.Should().BeTrue();
        testSubject.DisplayRegion.Should().Be(region.Name);
    }

    [TestMethod]
    public void ConnectedModeUiServices_Set_RaisesPropertyChanged()
    {
        var connectedModeUiServices = Substitute.For<IConnectedModeUIServices>();
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.ConnectedModeUiServices = connectedModeUiServices;

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.ConnectedModeUiServices)));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.DisplayName)));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.ShouldDisplayRegion)));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.DisplayRegion)));
        testSubject.ConnectedModeUiServices.Should().BeSameAs(connectedModeUiServices);
    }

    [DynamicData(nameof(Regions))]
    [DataTestMethod]
    public void ShouldDisplayRegion_ShowCloudRegionSettingUnchecked_ReturnsFalse(CloudServerRegion region)
    {
        testSubject.ConnectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarCloud, region);
        sonarLintSettings.ShowCloudRegion.Returns(false);

        testSubject.ShouldDisplayRegion.Should().BeFalse();
    }

    [DynamicData(nameof(Regions))]
    [DataTestMethod]
    public void ShouldDisplayRegion_ShowCloudRegionSettingChecked_ReturnsTrue(CloudServerRegion region)
    {
        testSubject.ConnectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarCloud, region);
        sonarLintSettings.ShowCloudRegion.Returns(true);

        testSubject.ShouldDisplayRegion.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldDisplayRegion_SonarQube_ShowCloudRegionSettingChecked_ReturnsFalse()
    {
        testSubject.ConnectionInfo = new ConnectionInfo(Id, ConnectionServerType.SonarQube);
        sonarLintSettings.ShowCloudRegion.Returns(true);

        testSubject.ShouldDisplayRegion.Should().BeFalse();
    }

    private void VerifyDoesNotDisplayRegion()
    {
        testSubject.ShouldDisplayRegion.Should().BeFalse();
        testSubject.DisplayRegion.Should().BeEmpty();
    }

    private void MockConnectedModeUiServices()
    {
        connectedModeUIServices.SonarLintSettings.Returns(sonarLintSettings);
        sonarLintSettings.ShowCloudRegion.Returns(true);

        testSubject.ConnectedModeUiServices = connectedModeUIServices;
    }
}
