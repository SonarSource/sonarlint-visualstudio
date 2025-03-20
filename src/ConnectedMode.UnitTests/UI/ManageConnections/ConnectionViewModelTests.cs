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
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ManageConnections;

[TestClass]
public class ConnectionViewModelTests
{
    private readonly Connection sonarCloudConnection = new(new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud));
    private IServerConnectionWithInvalidTokenRepository serverConnectionWithInvalidTokenRepository;
    private ConnectionViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        serverConnectionWithInvalidTokenRepository = Substitute.For<IServerConnectionWithInvalidTokenRepository>();
        testSubject = new ConnectionViewModel(sonarCloudConnection, serverConnectionWithInvalidTokenRepository);
    }

    [TestMethod]
    public void Ctor_InitializesProperties()
    {
        testSubject.Connection.Should().Be(sonarCloudConnection);
        testSubject.EnableSmartNotifications.Should().Be(sonarCloudConnection.EnableSmartNotifications);
        testSubject.ServerType.Should().Be(sonarCloudConnection.Info.ServerType.ToString());
        testSubject.Name.Should().Be(sonarCloudConnection.Info.Id);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void EnableSmartNotifications_Setter_UpdatesConnectionAndRaisesPropertyChanged(bool enableSmartNotifications)
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.EnableSmartNotifications = enableSmartNotifications;

        testSubject.Connection.EnableSmartNotifications.Should().Be(enableSmartNotifications);
        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.EnableSmartNotifications)));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void HasInvalidToken_ReturnsTrueOnlyIfConnectionIdExistsInRepository(bool hasInvalidToken)
    {
        serverConnectionWithInvalidTokenRepository.HasInvalidToken(sonarCloudConnection.ToServerConnection().Id).Returns(hasInvalidToken);

        testSubject.HasInvalidToken.Should().Be(hasInvalidToken);
    }

    [TestMethod]
    public void RefreshInvalidToken_RaisesPropertyChangedForHasInvalidToken()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.RefreshInvalidToken();

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.HasInvalidToken)));
    }
}
