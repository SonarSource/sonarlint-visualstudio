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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.UnitTests.State;

[TestClass]
public class AliveConnectionTrackerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AliveConnectionTracker, IAliveConnectionTracker>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
            MefTestHelpers.CreateExport<IServerConnectionsProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<AliveConnectionTracker>();
    }

    [TestMethod]
    public void Ctor_SubscribesToEvents()
    {
        var serverConnectionsRepository = new Mock<IServerConnectionsRepository>();
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out _, out _);

        CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), Mock.Of<IServerConnectionsProvider>(), serverConnectionsRepository.Object,
            asyncLockFactoryMock.Object);

        serverConnectionsRepository.VerifyAdd(x => x.ConnectionChanged += It.IsAny<EventHandler>());
        serverConnectionsRepository.VerifyAdd(x => x.CredentialsChanged += It.IsAny<EventHandler<ServerConnectionUpdatedEventArgs>>());
        asyncLockFactoryMock.Verify(x => x.Create());
    }

    [TestMethod]
    public void Refresh_SonarQubeConnection_CorrectlyUpdated()
    {
        const string connectionId = "connectionid";
        const string serverUrl = "http://localhost/";
        var sonarQubeConnection = new SonarQubeConnectionConfigurationDto(connectionId, true, serverUrl);
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        ConfigureConnectionProvider(out var connectionProviderMock, sonarQubeConnection);
        var testSubject = CreateTestSubject(serviceProviderMock.Object, connectionProviderMock.Object,
             Mock.Of<IServerConnectionsRepository>(), asyncLockFactoryMock.Object);

        testSubject.RefreshConnectionList();

        connectionServiceMock.Verify(x => x.DidUpdateConnections(
            It.Is<DidUpdateConnectionsParams>(p =>
                p.sonarCloudConnections.Count == 0
                && p.sonarQubeConnections.Count == 1
                && p.sonarQubeConnections.First().connectionId == connectionId
                && p.sonarQubeConnections.First().serverUrl == serverUrl)));
        connectionServiceMock.Verify(x => x.DidChangeCredentials(
            It.Is<DidChangeCredentialsParams>(p =>
                p.connectionId == connectionId)));
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void Refresh_SonarCloudConnection_CorrectlyUpdated()
    {
        const string connectionId = "connectionid";
        const string organization = "org";
        var sonarCloudConnection = new SonarCloudConnectionConfigurationDto(connectionId, true, organization);
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        ConfigureConnectionProvider(out var connectionProviderMock, sonarCloudConnection);
        var testSubject = CreateTestSubject(serviceProviderMock.Object, connectionProviderMock.Object,
            Mock.Of<IServerConnectionsRepository>(), asyncLockFactoryMock.Object);

        testSubject.RefreshConnectionList();

        connectionServiceMock.Verify(x => x.DidUpdateConnections(
            It.Is<DidUpdateConnectionsParams>(p =>
                p.sonarCloudConnections.Count == 1
                && p.sonarQubeConnections.Count == 0
                && p.sonarCloudConnections.First().connectionId == connectionId
                && p.sonarCloudConnections.First().organization == organization)));
        connectionServiceMock.Verify(x => x.DidChangeCredentials(
            It.Is<DidChangeCredentialsParams>(p =>
                p.connectionId == connectionId)));
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void Refresh_ChecksThread()
    {
        ConfigureServiceProvider(out var serviceProviderMock, out _);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out _, out _);
        ConfigureConnectionProvider(out var connectionProviderMock);
        var threadHandlingMock = new Mock<IThreadHandling>();
        var testSubject = CreateTestSubject(serviceProviderMock.Object, connectionProviderMock.Object,
            Mock.Of<IServerConnectionsRepository>(), asyncLockFactoryMock.Object, threadHandlingMock.Object);

        testSubject.RefreshConnectionList();

        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread());
    }

    [TestMethod]
    public void Refresh_ServiceUnavailable_Throws()
    {
        var serviceProviderMock = new Mock<ISLCoreServiceProvider>();
        var testSubject = CreateTestSubject(serviceProviderMock.Object, Mock.Of<IServerConnectionsProvider>(),
            Mock.Of<IServerConnectionsRepository>(), Mock.Of<IAsyncLockFactory>());

        var act = () => testSubject.RefreshConnectionList();

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void Event_TriggersRefresh()
    {
        var serverConnectionsRepository = new Mock<IServerConnectionsRepository>();
        var sonarQubeConnection = new SonarQubeConnectionConfigurationDto("sq1", true, "http://localhost/");
        var sonarCloudConnection = new SonarCloudConnectionConfigurationDto("sc2", true, "org");
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        ConfigureConnectionProvider(out var connectionProviderMock, sonarQubeConnection, sonarCloudConnection);
        CreateTestSubject(serviceProviderMock.Object, connectionProviderMock.Object,
            serverConnectionsRepository.Object, asyncLockFactoryMock.Object);

        serverConnectionsRepository.Raise(x => x.ConnectionChanged += It.IsAny<EventHandler>(), EventArgs.Empty);

        connectionServiceMock.Verify(x => x.DidUpdateConnections(
            It.Is<DidUpdateConnectionsParams>(p =>
                p.sonarCloudConnections.Count == 1
                && p.sonarQubeConnections.Count == 1)));
        connectionServiceMock.Verify(x => x.DidChangeCredentials(It.IsAny<DidChangeCredentialsParams>()),
            Times.Exactly(2));
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void Event_RunsOnBackgroundThread()
    {
        var serverConnectionsRepository = new Mock<IServerConnectionsRepository>();
        var threadHandlingMock = new Mock<IThreadHandling>();
        ConfigureConnectionProvider(out var connectionProviderMock);
        CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), connectionProviderMock.Object,
            serverConnectionsRepository.Object, Mock.Of<IAsyncLockFactory>(), threadHandlingMock.Object);

        serverConnectionsRepository.Raise(x => x.ConnectionChanged += It.IsAny<EventHandler>(), EventArgs.Empty);

        threadHandlingMock.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<int>>>()));
    }

    [TestMethod]
    public void Dispose_UnsubscribesAndDisposesLock()
    {
        var serverConnectionsRepository = new Mock<IServerConnectionsRepository>();
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out _);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), Mock.Of<IServerConnectionsProvider>(),
            serverConnectionsRepository.Object, asyncLockFactoryMock.Object);

        testSubject.Dispose();

        serverConnectionsRepository.VerifyRemove(x => x.ConnectionChanged -= It.IsAny<EventHandler>());
        asyncLockMock.Verify(x => x.Dispose());
    }

    [TestMethod]
    public void UpdateCredentials_ChecksThread()
    {
        ConfigureServiceProvider(out var serviceProviderMock, out _);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out _, out _);
        ConfigureConnectionProvider(out var connectionProviderMock);
        var threadHandlingMock = new Mock<IThreadHandling>();
        var testSubject = CreateTestSubject(serviceProviderMock.Object, connectionProviderMock.Object, Mock.Of<IServerConnectionsRepository>(), asyncLockFactory: asyncLockFactoryMock.Object, threadHandlingMock.Object);

        testSubject.UpdateCredentials("connId");

        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread());
    }

    [TestMethod]
    public void UpdateCredentials_ServiceUnavailable_Throws()
    {
        var serviceProviderMock = new Mock<ISLCoreServiceProvider>();
        var testSubject = CreateTestSubject(serviceProviderMock.Object, Mock.Of<IServerConnectionsProvider>(), Mock.Of<IServerConnectionsRepository>(), Mock.Of<IAsyncLockFactory>());

        var act = () => testSubject.UpdateCredentials("connId");

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void UpdateCredentials_SonarCloud_TriggersRefreshCredentials()
    {
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        var serverConnectionRepository = new Mock<IServerConnectionsRepository>();
        CreateTestSubject(serviceProviderMock.Object, Mock.Of<IServerConnectionsProvider>(), serverConnectionRepository.Object, asyncLockFactoryMock.Object);
        var sonarCloud = new ServerConnection.SonarCloud("myOrg");

        serverConnectionRepository.Raise(
            x => x.CredentialsChanged += It.IsAny<EventHandler<ServerConnectionUpdatedEventArgs>>(), new ServerConnectionUpdatedEventArgs(sonarCloud));

        connectionServiceMock.Verify(
            x => x.DidChangeCredentials(It.Is<DidChangeCredentialsParams>(args => args.connectionId == sonarCloud.Id)), Times.Once);
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void UpdateCredentials_SonarQube_TriggersRefreshCredentials()
    {
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        var serverConnectionRepository = new Mock<IServerConnectionsRepository>();
        CreateTestSubject(serviceProviderMock.Object, Mock.Of<IServerConnectionsProvider>(), serverConnectionRepository.Object, asyncLockFactoryMock.Object);
        var sonarQube = new ServerConnection.SonarQube(new Uri("http://localhost:9000"));

        serverConnectionRepository.Raise(
            x => x.CredentialsChanged += It.IsAny<EventHandler<ServerConnectionUpdatedEventArgs>>(), new ServerConnectionUpdatedEventArgs(sonarQube));

        connectionServiceMock.Verify(
            x => x.DidChangeCredentials(It.Is<DidChangeCredentialsParams>(args => args.connectionId == sonarQube.Id)), Times.Once);
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    private static void VerifyLockTakenAndReleased(Mock<IAsyncLock> asyncLock, Mock<IReleaseAsyncLock> lockRelease)
    {
        asyncLock.Verify(x => x.Acquire(), Times.Once);
        lockRelease.Verify(x => x.Dispose(), Times.Once);
    }

    private static void ConfigureConnectionProvider(out Mock<IServerConnectionsProvider> connectionProvider,
        params ServerConnectionConfiguration[] connections)
    {
        connectionProvider = new Mock<IServerConnectionsProvider>();
        connectionProvider.Setup(x => x.GetServerConnections()).Returns(connections.ToDictionary(x => x.connectionId, x => x));
    }

    private static void ConfigureAsyncLockFactory(out Mock<IAsyncLockFactory> asyncLockFactory,
        out Mock<IAsyncLock> asyncLock, out Mock<IReleaseAsyncLock> asyncLockRelease)
    {
        asyncLockRelease = new();
        asyncLock = new();
        asyncLock.Setup(x => x.Acquire()).Returns(asyncLockRelease.Object);
        asyncLockFactory = new();
        asyncLockFactory.Setup(x => x.Create()).Returns(asyncLock.Object);
    }

    private static void ConfigureServiceProvider(out Mock<ISLCoreServiceProvider> serviceProvider,
        out Mock<IConnectionConfigurationSLCoreService> connectionService)
    {
        serviceProvider = new Mock<ISLCoreServiceProvider>();
        connectionService = new Mock<IConnectionConfigurationSLCoreService>();
        var service = connectionService.Object;
        serviceProvider.Setup(x => x.TryGetTransientService(out service)).Returns(true);
    }

    private static AliveConnectionTracker CreateTestSubject(ISLCoreServiceProvider slCoreServiceProvider,
        IServerConnectionsProvider serverConnectionsProvider,
        IServerConnectionsRepository connectionsRepository,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling = null)
    {
        return new AliveConnectionTracker(slCoreServiceProvider,
            serverConnectionsProvider,
            connectionsRepository,
            asyncLockFactory,
            threadHandling ?? new NoOpThreadHandler());
    }
}
