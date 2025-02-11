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
using SonarLint.VisualStudio.ConnectedMode.UI.ConnectionDisplay;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ConnectionDisplay;

[TestClass]
public class ConnectionNameViewModelTests
{
    private ConnectionNameViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new ConnectionNameViewModel();
    }

    [TestMethod]
    public void ConnectionInfo_Set_RaisesPropertyChanged()
    {
        var connectionInfo = new ConnectionInfo("id", ConnectionServerType.SonarQube);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.ConnectionInfo = connectionInfo;

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == "ConnectionInfo"));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == "DisplayName"));
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
        testSubject.ConnectionInfo = new ConnectionInfo("id", ConnectionServerType.SonarQube);

        testSubject.DisplayName.Should().Be("id");
    }

    [DataRow("EU")]
    [DataRow("US")]
    [DataTestMethod]
    public void DisplayName_SonarCloud_NoId_HasRegion_ReturnsUrl(string region)
    {
        var cloudServerRegion = CloudServerRegion.GetRegionByName(region);
        testSubject.ConnectionInfo = new ConnectionInfo(null, ConnectionServerType.SonarCloud, cloudServerRegion);

        testSubject.DisplayName.Should().Be(cloudServerRegion.Url.ToString());
    }

    [TestMethod]
    public void DisplayName_SonarCloud_HasId_ReturnsUrl()
    {
        testSubject.ConnectionInfo = new ConnectionInfo("id", ConnectionServerType.SonarCloud, CloudServerRegion.Eu);

        testSubject.DisplayName.Should().Be("id");
    }
}
