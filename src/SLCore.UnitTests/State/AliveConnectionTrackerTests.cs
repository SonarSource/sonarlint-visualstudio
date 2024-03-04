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
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

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
            MefTestHelpers.CreateExport<IConnectionIdHelper>(),
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
        ConfigureServiceProvider(out var serviceProviderMock, out _);
        ConfigureBindingRepository(out var bindingRepositoryMock);
        ConfigureConnectionIdHelper(out var connectionIdHelperMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out _, out _);

        CreateTestSubject(serviceProviderMock.Object, bindingRepositoryMock.Object, connectionIdHelperMock.Object, asyncLockFactoryMock.Object);

        bindingRepositoryMock.VerifyAdd(x => x.BindingUpdated += It.IsAny<EventHandler>());
        asyncLockFactoryMock.Verify(x => x.Create());
    }

    [TestMethod]
    public void Refresh_SonarQubeConnection_CorrectlyUpdated()
    {
        const string idToReturn = "connectionid";
        var boundSonarQubeProject = new BoundSonarQubeProject(new Uri("http://localhost/"), "project", default);
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureBindingRepository(out var bindingRepositoryMock, boundSonarQubeProject);
        ConfigureConnectionIdHelper(out var connectionIdHelperMock, boundSonarQubeProject, idToReturn);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        var testSubject = CreateTestSubject(serviceProviderMock.Object, bindingRepositoryMock.Object,
            connectionIdHelperMock.Object, asyncLockFactoryMock.Object);

        testSubject.RefreshConnectionList();

        connectionServiceMock.Verify(x => x.DidUpdateConnections(It.Is<DidUpdateConnectionsParams>(p =>
            p.sonarCloudConnections.Count == 0
            && p.sonarQubeConnections.Count == 1
            && p.sonarQubeConnections.First().connectionId == idToReturn
            && p.sonarQubeConnections.First().serverUrl == "http://localhost/")));
        connectionServiceMock.Verify(x => x.DidChangeCredentials(It.Is<DidChangeCredentialsParams>(p =>
            p.connectionId == idToReturn)));
        connectionIdHelperMock.Verify(x => x.GetConnectionIdFromUri(boundSonarQubeProject.ServerUri, null));
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void Refresh_SonarCloudConnection_CorrectlyUpdated()
    {
        const string idToReturn = "connectionid";
        var boundSonarCloudProject = new BoundSonarQubeProject(new Uri("https://sonarcloud.io/"), "project", default,
            organization: new SonarQubeOrganization("org", default));
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureBindingRepository(out var bindingRepositoryMock, boundSonarCloudProject);
        ConfigureConnectionIdHelper(out var connectionIdHelperMock, boundSonarCloudProject, idToReturn);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        var testSubject = CreateTestSubject(serviceProviderMock.Object, bindingRepositoryMock.Object,
            connectionIdHelperMock.Object, asyncLockFactoryMock.Object);

        testSubject.RefreshConnectionList();

        connectionServiceMock.Verify(x => x.DidUpdateConnections(It.Is<DidUpdateConnectionsParams>(p =>
            p.sonarCloudConnections.Count == 1
            && p.sonarQubeConnections.Count == 0
            && p.sonarCloudConnections.First().connectionId == idToReturn
            && p.sonarCloudConnections.First().organization == boundSonarCloudProject.Organization.Key)));
        connectionServiceMock.Verify(x => x.DidChangeCredentials(It.Is<DidChangeCredentialsParams>(p =>
            p.connectionId == idToReturn)));
        connectionIdHelperMock.Verify(x =>
            x.GetConnectionIdFromUri(boundSonarCloudProject.ServerUri, boundSonarCloudProject.Organization.Key));
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void Refresh_DuplicateConnectionIds_Aggregates()
    {
        var project1 = new BoundSonarQubeProject(new Uri("https://sonarcloud.io/"), "projectaaaa", default,
            organization: new SonarQubeOrganization("org", default));
        var project1duplicate = new BoundSonarQubeProject(new Uri("https://sonarcloud.io/"), "projectbbb", default,
            organization: new SonarQubeOrganization("org", default));
        var project2 = new BoundSonarQubeProject(new Uri("http://localhost/"), "projectccc", default);
        var project2duplicate = new BoundSonarQubeProject(new Uri("http://localhost/"), "projectddd", default);
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureBindingRepository(out var bindingRepositoryMock, project1, project2, project1duplicate, project2duplicate);
        ConfigureConnectionIdHelper(out var connectionIdHelperMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        var testSubject = CreateTestSubject(serviceProviderMock.Object, bindingRepositoryMock.Object,
            connectionIdHelperMock.Object, asyncLockFactoryMock.Object);

        testSubject.RefreshConnectionList();

        connectionServiceMock.Verify(x => x.DidUpdateConnections(It.Is<DidUpdateConnectionsParams>(p =>
            p.sonarCloudConnections.Count == 1
            && p.sonarQubeConnections.Count == 1)));
        connectionServiceMock.Verify(x => x.DidChangeCredentials(It.IsAny<DidChangeCredentialsParams>()),
            Times.Exactly(2));
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void Refresh_ChecksThread()
    {
        ConfigureServiceProvider(out var serviceProviderMock, out _);
        ConfigureBindingRepository(out var bindingRepositoryMock);
        ConfigureConnectionIdHelper(out var connectionIdHelperMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out _, out _);
        var threadHandlingMock = new Mock<IThreadHandling>();
        var testSubject = CreateTestSubject(serviceProviderMock.Object, bindingRepositoryMock.Object,
            connectionIdHelperMock.Object, asyncLockFactoryMock.Object, threadHandlingMock.Object);

        testSubject.RefreshConnectionList();

        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread());
    }

    [TestMethod]
    public void Refresh_ServiceUnavailable_Throws()
    {
        var serviceProviderMock = new Mock<ISLCoreServiceProvider>();
        var testSubject = CreateTestSubject(serviceProviderMock.Object, Mock.Of<ISolutionBindingRepository>(),
            Mock.Of<IConnectionIdHelper>(), Mock.Of<IAsyncLockFactory>());

        var act = () => testSubject.RefreshConnectionList();

        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage(Strings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void Event_TriggersRefresh()
    {
        var project1 = new BoundSonarQubeProject(new Uri("https://sonarcloud.io/"), "projectaaaa", default,
            organization: new SonarQubeOrganization("org", default));
        var project2 = new BoundSonarQubeProject(new Uri("http://localhost/"), "projectccc", default);
        ConfigureServiceProvider(out var serviceProviderMock, out var connectionServiceMock);
        ConfigureBindingRepository(out var bindingRepositoryMock, project1, project2);
        ConfigureConnectionIdHelper(out var connectionIdHelperMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out var asyncLockReleaseMock);
        var testSubject = CreateTestSubject(serviceProviderMock.Object, bindingRepositoryMock.Object,
            connectionIdHelperMock.Object, asyncLockFactoryMock.Object);

        bindingRepositoryMock.Raise(x => x.BindingUpdated += It.IsAny<EventHandler>(), EventArgs.Empty);

        connectionServiceMock.Verify(x => x.DidUpdateConnections(It.Is<DidUpdateConnectionsParams>(p =>
            p.sonarCloudConnections.Count == 1
            && p.sonarQubeConnections.Count == 1)));
        connectionServiceMock.Verify(x => x.DidChangeCredentials(It.IsAny<DidChangeCredentialsParams>()),
            Times.Exactly(2));
        VerifyLockTakenAndReleased(asyncLockMock, asyncLockReleaseMock);
    }

    [TestMethod]
    public void Event_RunsOnBackgroundThread()
    {
        ConfigureBindingRepository(out var bindingRepositoryMock);
        var threadHandlingMock = new Mock<IThreadHandling>();
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), bindingRepositoryMock.Object,
            Mock.Of<IConnectionIdHelper>(), Mock.Of<IAsyncLockFactory>(), threadHandlingMock.Object);
        
        bindingRepositoryMock.Raise(x => x.BindingUpdated += It.IsAny<EventHandler>(), EventArgs.Empty);
        
        threadHandlingMock.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<int>>>()));
    }

    [TestMethod]
    public void Dispose_UnsubscribesAndDisposesLock()
    {
        ConfigureBindingRepository(out var bindingRepositoryMock);
        ConfigureAsyncLockFactory(out var asyncLockFactoryMock, out var asyncLockMock, out _);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), bindingRepositoryMock.Object,
            Mock.Of<IConnectionIdHelper>(), asyncLockFactoryMock.Object);
        
        testSubject.Dispose();
        
        bindingRepositoryMock.VerifyRemove(x => x.BindingUpdated -= It.IsAny<EventHandler>());
        asyncLockMock.Verify(x => x.Dispose());
    }

    private static void VerifyLockTakenAndReleased(Mock<IAsyncLock> asyncLock, Mock<IReleaseAsyncLock> lockRelease)
    {
        asyncLock.Verify(x => x.Acquire(), Times.Once);
        lockRelease.Verify(x => x.Dispose(), Times.Once);
    }

    private static void ConfigureBindingRepository(out Mock<ISolutionBindingRepository> bindingRepository,
        params BoundSonarQubeProject[] bindings)
    {
        bindingRepository = new();
        bindingRepository.Setup(x => x.List()).Returns(bindings);
    }

    private static void ConfigureConnectionIdHelper(out Mock<IConnectionIdHelper> connectionIdHelper,
        BoundSonarQubeProject binding = null, string idToReturn = null)
    {
        connectionIdHelper = new();

        if (binding is null)
        {
            connectionIdHelper
                .Setup(x => x.GetConnectionIdFromUri(It.IsAny<Uri>(), It.IsAny<string>()))
                .Returns((Uri t1, string t2) => new ConnectionIdHelper().GetConnectionIdFromUri(t1, t2));
        }
        else
        {
            connectionIdHelper
                .Setup(x => x.GetConnectionIdFromUri(binding.ServerUri,
                    binding.Organization == null ? null : binding.Organization.Key))
                .Returns(idToReturn);
        }
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
        ISolutionBindingRepository bindingRepository,
        IConnectionIdHelper connectionIdHelper,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling = null)
    {
        return new AliveConnectionTracker(slCoreServiceProvider,
            bindingRepository,
            connectionIdHelper,
            asyncLockFactory,
            threadHandling ?? new NoOpThreadHandler());
    }
}
