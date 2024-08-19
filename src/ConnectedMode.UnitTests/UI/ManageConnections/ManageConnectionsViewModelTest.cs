/*
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
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ManageConnections;

[TestClass]
public class ManageConnectionsViewModelTest
{
    private ManageConnectionsViewModel testSubject;
    private IEnumerable<Connection> connections;

    [TestInitialize]
    public void TestInitialize()
    {
        connections =
        [
            new Connection(new ConnectionInfo("http://localhost:9000", ConnectionServerType.SonarQube), true),
            new Connection(new ConnectionInfo("https://sonarcloud.io/myOrg", ConnectionServerType.SonarCloud), false)
        ];
        testSubject = new ManageConnectionsViewModel();
    }

    [TestMethod]
    public void ConnectionViewModels_NoInitialization_HasEmptyList()
    {
        testSubject.ConnectionViewModels.Should().NotBeNull();
        testSubject.ConnectionViewModels.Count.Should().Be(0);
    }

    [TestMethod]
    public void InitializeConnections_InitializesConnectionsCorrectly()
    { 
        testSubject.InitializeConnections(connections);

        HasExpectedConnections(connections);
    }

    [TestMethod]
    public void RemoveConnection_RemovesProvidedConnection()
    {
        testSubject.InitializeConnections(connections);
        var connectionToRemove = testSubject.ConnectionViewModels.First();

        testSubject.RemoveConnection(connectionToRemove);

        testSubject.ConnectionViewModels.Count.Should().Be(connections.Count() - 1);
        testSubject.ConnectionViewModels.Should().NotContain(connectionToRemove);
    }

    [TestMethod]
    public void RemoveConnection_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        testSubject.InitializeConnections(connections);

        testSubject.RemoveConnection(testSubject.ConnectionViewModels[0]);

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoConnectionExists)));
    }

    [TestMethod]
    public void AddConnection_AddsProvidedConnection()
    {
        testSubject.InitializeConnections(connections);
        var connectionToAdd = new Connection(new ConnectionInfo("https://sonarcloud.io/mySecondOrg", ConnectionServerType.SonarCloud), false);

        testSubject.AddConnection(connectionToAdd);

        var viewModelsCount = testSubject.ConnectionViewModels.Count;
        viewModelsCount.Should().Be(connections.Count() + 1);
        testSubject.ConnectionViewModels[viewModelsCount - 1].Connection.Should().Be(connectionToAdd);
    }

    [TestMethod]
    public void AddConnection_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        testSubject.InitializeConnections(connections);

        testSubject.AddConnection(new Connection(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud), false));

        eventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoConnectionExists)));
    }

    [TestMethod]
    public void NoConnectionExists_NoConnections_ReturnsTrue()
    {
        testSubject.InitializeConnections([]);

        testSubject.NoConnectionExists.Should().BeTrue();
    }

    [TestMethod]
    public void NoConnectionExists_HasConnections_ReturnsFalse()
    {
        testSubject.InitializeConnections(connections);

        testSubject.NoConnectionExists.Should().BeFalse();
    }

    private void HasExpectedConnections(IEnumerable<Connection> expectedConnections)
    {
        testSubject.ConnectionViewModels.Should().NotBeNull();
        testSubject.ConnectionViewModels.Count.Should().Be(connections.Count());
        foreach (var connection in expectedConnections)
        {
            var connectionViewModel = testSubject.ConnectionViewModels.SingleOrDefault(c => c.Name == connection.Info.Id);
            connectionViewModel.Should().NotBeNull();
            connectionViewModel.ServerType.Should().Be(connection.Info.ServerType.ToString());
            connectionViewModel.EnableSmartNotifications.Should().Be(connection.EnableSmartNotifications);
        }
    }
}
