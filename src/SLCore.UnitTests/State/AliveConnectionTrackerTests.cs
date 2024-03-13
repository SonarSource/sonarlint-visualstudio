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

using System;
using System.Linq;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests.State;

[TestClass]
public class AliveConnectionTrackerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AliveConnectionTracker, IAliveConnectionTracker>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<ISolutionBindingRepository>(),
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
        var bindingRepositoryMock = new Mock<ISolutionBindingRepository>();
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out _, out _);

        CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), Mock.Of<IServerConnectionsProvider>(), bindingRepositoryMock.Object,
            asyncLockFactoryMock.Object);

        bindingRepositoryMock.VerifyAdd(x => x.BindingUpdated += It.IsAny<EventHandler>());
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
             Mock.Of<ISolutionBindingRepository>(), asyncLockFactoryMock.Object);

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
            Mock.Of<ISolutionBindingRepository>(), asyncLockFactoryMock.Object);

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
            Mock.Of<ISolutionBindingRepository>(), asyncLockFactoryMock.Object, threadHandlingMock.Object);

        testSubject.RefreshConnectionList();

        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread());
    }

    [TestMethod]
    public void Refresh_ServiceUnavailable_Throws()
    {
        var serviceProviderMock = new Mock<ISLCoreServiceProvider>();
        var testSubject = CreateTestSubject(serviceProviderMock.Object, Mock.Of<IServerConnectionsProvider>(),
            Mock.Of<ISolutionBindingRepository>(), Mock.Of<IAsyncLockFactory>());

        var act = () => testSubject.RefreshConnectionList();

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(Strings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void Event_TriggersRefresh()
    {
        var bindingRepositoryMock = new Mock<ISolutionBindingRepository>();
        var sonarQubeConnection = new SonarQubeConnectionConfigurationDto("sq1", true, "http://localhost/");
        var sonarCloudConnection = new SonarCloudConnectionConfigurationDto("sc2", true, "org");
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        ConfigureConnectionProvider(out var connectionProviderMock, sonarQubeConnection, sonarCloudConnection);
        var testSubject = CreateTestSubject(serviceProviderMock.Object, connectionProviderMock.Object,
            bindingRepositoryMock.Object, asyncLockFactoryMock.Object);

        bindingRepositoryMock.Raise(x => x.BindingUpdated += It.IsAny<EventHandler>(), EventArgs.Empty);

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
        var bindingRepositoryMock = new Mock<ISolutionBindingRepository>();
        var threadHandlingMock = new Mock<IThreadHandling>();
        ConfigureConnectionProvider(out var connectionProviderMock);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), connectionProviderMock.Object,
            bindingRepositoryMock.Object, Mock.Of<IAsyncLockFactory>(), threadHandlingMock.Object);

        bindingRepositoryMock.Raise(x => x.BindingUpdated += It.IsAny<EventHandler>(), EventArgs.Empty);

        threadHandlingMock.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<int>>>()));
    }

    [TestMethod]
    public void Dispose_UnsubscribesAndDisposesLock()
    {
        var bindingRepositoryMock = new Mock<ISolutionBindingRepository>();
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out _);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), Mock.Of<IServerConnectionsProvider>(),
            bindingRepositoryMock.Object, asyncLockFactoryMock.Object);

        testSubject.Dispose();

        bindingRepositoryMock.VerifyRemove(x => x.BindingUpdated -= It.IsAny<EventHandler>());
        asyncLockMock.Verify(x => x.Dispose());
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
        ISolutionBindingRepository bindingRepository,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling = null)
    {
        return new AliveConnectionTracker(slCoreServiceProvider,
            serverConnectionsProvider,
            bindingRepository,
            asyncLockFactory,
            threadHandling ?? new NoOpThreadHandler());
    }
}
