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

using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class ExistingConnectionForBindingProviderTests
{
    private IServerConnectionsRepositoryAdapter connectionsRepositoryAdapter;
    private ExistingConnectionForBindingProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        connectionsRepositoryAdapter = Substitute.For<IServerConnectionsRepositoryAdapter>();
        testSubject = new ExistingConnectionForBindingProvider(connectionsRepositoryAdapter);
    }

    [TestMethod]
    public async Task GetServerConnectionAsync_ConnectionExists_ReturnsConnection()
    {
        var serverConnectionId = "connection id";
        var fakeConnection = new ServerConnection.SonarCloud("fake connection");
        connectionsRepositoryAdapter.TryGet(serverConnectionId, out Arg.Any<ServerConnection>()).Returns(x =>
        {
            x[1] = fakeConnection;
            return true;
        });

        var result = await testSubject.GetServerConnectionAsync(new BindingRequest.Manual("any", serverConnectionId));

        result.Should().BeSameAs(fakeConnection);
    }

    [TestMethod]
    public async Task GetServerConnectionAsync_ConnectionDoesNotExist_ReturnsNull()
    {
        var serverConnectionId = "connection id";
        connectionsRepositoryAdapter.TryGet(serverConnectionId, out Arg.Any<ServerConnection>()).Returns(false);

        var result = await testSubject.GetServerConnectionAsync(new BindingRequest.Manual("any", serverConnectionId));

        result.Should().BeNull();
    }
}
