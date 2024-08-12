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

using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

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
            new Connection("http://localhost:9000", ServerType.SonarQube, true),
            new Connection("https://sonarcloud.io/myOrg", ServerType.SonarCloud, false)
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

    private void HasExpectedConnections(IEnumerable<Connection> expectedConnections)
    {
        testSubject.ConnectionViewModels.Should().NotBeNull();
        testSubject.ConnectionViewModels.Count.Should().Be(connections.Count());
        foreach (var connection in expectedConnections)
        {
            var connectionViewModel = testSubject.ConnectionViewModels.SingleOrDefault(c => c.Name == connection.Id);
            connectionViewModel.Should().NotBeNull();
            connectionViewModel.ServerType.Should().Be(connection.ServerType.ToString());
            connectionViewModel.EnableSmartNotifications.Should().Be(connection.EnableSmartNotifications);
        }
    }
}
