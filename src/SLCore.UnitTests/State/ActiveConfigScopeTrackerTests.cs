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
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Project;
using SonarLint.VisualStudio.SLCore.Service.Project.Models;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests.State;

[TestClass]
public class ActiveConfigScopeTrackerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ActiveConfigScopeTracker, IActiveConfigScopeTracker>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ActiveConfigScopeTracker>();
    }


    [TestMethod]
    public async Task SetCurrentConfigScope_SetsUnboundScope()
    {
        const string configScopeId = "myid";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, lockFactory.Object, threadHandling.Object);

        await testSubject.SetCurrentConfigScopeAsync(configScopeId);

        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScopeDto(configScopeId, configScopeId, true, null));
        VerifyThreadHandling(threadHandling);
        VerifyServiceAddCall(configScopeService, testSubject);
        VerifyLockTakenAndReleased(asyncLock, lockRelease);
    }
    
    [TestMethod]
    public async Task SetCurrentConfigScope_SetsBoundScope()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, lockFactory.Object, threadHandling.Object);

        await testSubject.SetCurrentConfigScopeAsync(configScopeId, connectionId, sonarProjectKey);

        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScopeDto(configScopeId, configScopeId, true, new BindingConfigurationDto(connectionId, sonarProjectKey)));
        VerifyThreadHandling(threadHandling);
        VerifyServiceAddCall(configScopeService, testSubject);
        VerifyLockTakenAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public async Task SetCurrentConfigScope_ServiceUnavailable_Throws()
    {
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), lockFactory.Object, Mock.Of<IThreadHandling>());

        var act = () => testSubject.SetCurrentConfigScopeAsync("id");

        await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage(Strings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task RemoveCurrentConfigScope_RemovesScope()
    {
        const string configScopeId = "myid";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, lockFactory.Object, threadHandling.Object);
        testSubject.currentConfigScope = new ConfigurationScopeDto(configScopeId, configScopeId, true, null);
        
        await testSubject.RemoveCurrentConfigScopeAsync();
        
        VerifyThreadHandling(threadHandling);
        configScopeService.Verify(x => x.DidRemoveConfigurationScopeAsync(It.Is<DidRemoveConfigurationScopeParams>(p => p.removeId == configScopeId)));
        VerifyLockTakenAndReleased(asyncLock, lockRelease);
    }
    
    [TestMethod]
    public async Task RemoveCurrentConfigScope_ServiceUnavailable_Throws()
    {
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), lockFactory.Object, Mock.Of<IThreadHandling>());
        testSubject.currentConfigScope = new ConfigurationScopeDto(default, default, default, default);

        var act = () => testSubject.RemoveCurrentConfigScopeAsync();

        await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage(Strings.ServiceProviderNotInitialized);
    }
    
    [TestMethod]
    public void GetCurrent_ReturnsUnboundScope()
    {
        const string configScopeId = "myid";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, lockFactory.Object, threadHandling.Object);
        testSubject.currentConfigScope = new ConfigurationScopeDto(configScopeId, configScopeId, true, null);

        var currentScope = testSubject.Current;

        currentScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId));
        VerifyThreadHandling(threadHandling);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }
    
    [TestMethod]
    public void GetCurrent_ReturnsBoundScope()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, lockFactory.Object, threadHandling.Object);
        testSubject.currentConfigScope = new ConfigurationScopeDto(configScopeId, configScopeId, true, new BindingConfigurationDto(connectionId, sonarProjectKey));

        var currentScope = testSubject.Current;

        currentScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey));
        VerifyThreadHandling(threadHandling);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    private static void VerifyThreadHandling(Mock<IThreadHandling> threadHandling)
    {
        threadHandling.Verify(x => x.ThrowIfOnUIThread());
    }

    private static void VerifyServiceAddCall(Mock<IConfigurationScopeSLCoreService> configScopeService, ActiveConfigScopeTracker testSubject)
    {
        configScopeService
            .Verify(x =>
                    x.DidAddConfigurationScopesAsync(It.Is<DidAddConfigurationScopesParams>(p =>
                        p.addedScopes.SequenceEqual(new[] { testSubject.currentConfigScope }))),
                Times.Once);
    }

    private static void VerifyLockTakenSynchronouslyAndReleased(Mock<IAsyncLock> asyncLock, Mock<IReleaseAsyncLock> lockRelease)
    {
        asyncLock.Verify(x => x.Acquire(), Times.Once);
        lockRelease.Verify(x => x.Dispose(), Times.Once);
    }    
    
    private static void VerifyLockTakenAndReleased(Mock<IAsyncLock> asyncLock, Mock<IReleaseAsyncLock> lockRelease)
    {
        asyncLock.Verify(x => x.AcquireAsync(), Times.Once);
        lockRelease.Verify(x => x.Dispose(), Times.Once);
    }

    private static void ConfigureAsyncLockFactory(out Mock<IAsyncLockFactory> asyncLockFactory,
        out Mock<IAsyncLock> asyncLock, out Mock<IReleaseAsyncLock> asyncLockRelease)
    {
        asyncLockRelease = new Mock<IReleaseAsyncLock>();
        asyncLock = new Mock<IAsyncLock>();
        asyncLock.Setup(x => x.AcquireAsync()).ReturnsAsync(asyncLockRelease.Object);
        asyncLock.Setup(x => x.Acquire()).Returns(asyncLockRelease.Object);
        asyncLockFactory = new Mock<IAsyncLockFactory>();
        asyncLockFactory.Setup(x => x.Create()).Returns(asyncLock.Object);
    }

    private static void ConfigureServiceProvider(out Mock<ISLCoreServiceProvider> serviceProvider,
        out Mock<IConfigurationScopeSLCoreService> configScopeService)
    {
        serviceProvider = new Mock<ISLCoreServiceProvider>();
        configScopeService = new Mock<IConfigurationScopeSLCoreService>();
        var service = configScopeService.Object;
        serviceProvider.Setup(x => x.TryGetTransientService(out service)).Returns(true);
    }

    private static ActiveConfigScopeTracker CreateTestSubject(ISLCoreServiceProvider slCoreServiceProvider,
        IAsyncLockFactory asyncLockFactory,
        IThreadHandling threadHandling)
    {
        return new ActiveConfigScopeTracker(slCoreServiceProvider, asyncLockFactory, threadHandling);
    }
}
