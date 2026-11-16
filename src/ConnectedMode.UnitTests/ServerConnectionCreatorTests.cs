/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class ServerConnectionCreatorTests
{
    private readonly Connection connection = new(new ConnectionInfo("http://localhost:9000", ConnectionServerType.SonarQube));
    private ISlCoreConnectionAdapter slCoreConnectionAdapter;
    private IServerConnectionsRepositoryAdapter repository;
    private ICredentialsModel credentials;
    private ServerConnectionCreator testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        repository = Substitute.For<IServerConnectionsRepositoryAdapter>();
        credentials = Substitute.For<ICredentialsModel>();
        testSubject = new ServerConnectionCreator(slCoreConnectionAdapter, repository);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ServerConnectionCreator, IServerConnectionCreator>(
            MefTestHelpers.CreateExport<ISlCoreConnectionAdapter>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepositoryAdapter>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ServerConnectionCreator>();

    [TestMethod]
    public async Task ValidateAsync_ReturnsTheSlCoreValidation()
    {
        var expected = new ResponseStatus(false, "invalid token");
        slCoreConnectionAdapter.ValidateConnectionAsync(connection.Info, credentials).Returns(expected);

        var response = await testSubject.ValidateAsync(connection.Info, credentials);

        response.Should().BeSameAs(expected);
    }

    [TestMethod]
    public void Add_ConnectionExists_FailsWithoutAdding()
    {
        repository.TryGet(connection.Info, out _).Returns(call =>
        {
            call[1] = new ServerConnection.SonarQube(new Uri(connection.Info.Id));
            return true;
        });

        var response = testSubject.Add(connection, credentials);

        response.Success.Should().BeFalse();
        response.WarningText.Should().Be(UiResources.ConnectionAlreadyExistsText);
        repository.DidNotReceiveWithAnyArgs().TryAddConnection(default, default);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Add_NewConnection_ReturnsWhetherTheRepositoryAddedIt(bool added)
    {
        repository.TryAddConnection(connection, credentials).Returns(added);

        var response = testSubject.Add(connection, credentials);

        response.Success.Should().Be(added);
        repository.Received(1).TryAddConnection(connection, credentials);
    }
}
