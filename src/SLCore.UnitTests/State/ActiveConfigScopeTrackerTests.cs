﻿/*
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Project;
using SonarLint.VisualStudio.SLCore.Service.Project.Models;
using SonarLint.VisualStudio.SLCore.Service.Project.Params;
using SonarLint.VisualStudio.SLCore.State;

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
    public void SetCurrentConfigScope_SetsUnboundScope()
    {
        const string configScopeId = "myid";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);

        testSubject.SetCurrentConfigScope(configScopeId);

        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId));
        VerifyThreadHandling(threadHandling);
        VerifyServiceAddCall(configScopeService, testSubject);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public void TryUpdateRootOnCurrentConfigScope_ConfigScopeSame_Updates()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const bool isReady = true;
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, isReadyForAnalysis: isReady);

        var result = testSubject.TryUpdateRootOnCurrentConfigScope(configScopeId, "some root");
        
        result.Should().BeTrue();
        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, "some root", isReady));
    }

    [TestMethod]
    public void TryUpdateRootOnCurrentConfigScope_ConfigScopeDifferent_DoesNotUpdate()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const bool isReady = true;
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, isReadyForAnalysis: isReady);

        var result = testSubject.TryUpdateRootOnCurrentConfigScope("some other id", "some root");
        
        result.Should().BeFalse();
        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, isReadyForAnalysis: isReady));
    }
    
    [TestMethod]
    public void TryUpdateAnalysisReadinessOnCurrentConfigScope_ConfigScopeSame_Updates()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const string root = "root";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root);

        var result = testSubject.TryUpdateAnalysisReadinessOnCurrentConfigScope(configScopeId, true);
        
        result.Should().BeTrue();
        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root, true));
    }

    [TestMethod]
    public void TryUpdateAnalysisReadinessOnCurrentConfigScope_ConfigScopeDifferent_DoesNotUpdate()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const string root = "root";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root);

        var result = testSubject.TryUpdateAnalysisReadinessOnCurrentConfigScope("some other id", true);
        
        result.Should().BeFalse();
        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root));
    }

    [TestMethod]
    public void SetCurrentConfigScope_SetsBoundScope()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);

        testSubject.SetCurrentConfigScope(configScopeId, connectionId, sonarProjectKey);

        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey));
        VerifyThreadHandling(threadHandling);
        VerifyServiceAddCall(configScopeService, testSubject);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public void SetCurrentConfigScope_CurrentScopeExists_UpdatesBoundScope()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        const string rootPath = "somepath";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        var existingConfigScope = new ConfigurationScope(configScopeId, RootPath: rootPath);
        testSubject.currentConfigScope = existingConfigScope;

        testSubject.SetCurrentConfigScope(configScopeId, connectionId, sonarProjectKey);

        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, rootPath));
        testSubject.currentConfigScope.Should().NotBeSameAs(existingConfigScope);
        VerifyThreadHandling(threadHandling);
        VerifyServiceUpdateCall(configScopeService, testSubject);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public void SetCurrentConfigScope_ServiceUnavailable_Throws()
    {
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), threadHandling.Object, lockFactory.Object);

        var act = () => testSubject.SetCurrentConfigScope("id");

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
        VerifyThreadHandling(threadHandling);
    }

    [TestMethod]
    public void SetCurrentConfigScope_UpdateConfigScopeWithDifferentId_Throws()
    {
        const string configScopeId = "myid";
        const string anotherConfigScopeId = "anotherid";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        var existingConfigScope = new ConfigurationScope(configScopeId);
        testSubject.currentConfigScope = existingConfigScope;

        var act = () => testSubject.SetCurrentConfigScope(anotherConfigScopeId);

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ConfigScopeConflict);
        VerifyThreadHandling(threadHandling);
    }

    [TestMethod]
    public void RemoveCurrentConfigScope_RemovesScope()
    {
        const string configScopeId = "myid";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId);

        testSubject.RemoveCurrentConfigScope();

        configScopeService.Verify(x => x.DidRemoveConfigurationScope(It.Is<DidRemoveConfigurationScopeParams>(p => p.removedId == configScopeId)));
        VerifyThreadHandling(threadHandling);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public void RemoveCurrentConfigScope_NoCurrentScope_DoesNothing()
    {
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out var configScopeService);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);

        testSubject.RemoveCurrentConfigScope();

        configScopeService.VerifyNoOtherCalls();
        VerifyThreadHandling(threadHandling);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public void RemoveCurrentConfigScope_ServiceUnavailable_Throws()
    {
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureAsyncLockFactory(out var lockFactory, out _, out _);
        var testSubject = CreateTestSubject(Mock.Of<ISLCoreServiceProvider>(), threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope("some Id", default, default, default);

        var act = () => testSubject.RemoveCurrentConfigScope();

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
        VerifyThreadHandling(threadHandling);
    }

    [TestMethod]
    public void GetCurrent_ReturnsUnboundScope()
    {
        const string configScopeId = "myid";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId);

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
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey);

        var currentScope = testSubject.Current;

        currentScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey));
        currentScope.RootPath.Should().Be(null); // not implemented
        VerifyThreadHandling(threadHandling);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public void Reset_SetsCurrentScopeToNull()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        var threadHandling = new Mock<IThreadHandling>();
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out var lockRelease);
        var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object, lockFactory.Object);
        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, configScopeId, connectionId, sonarProjectKey);

        testSubject.Reset();

        testSubject.currentConfigScope.Should().BeNull();
        serviceProvider.VerifyNoOtherCalls();
        VerifyThreadHandling(threadHandling);
        VerifyLockTakenSynchronouslyAndReleased(asyncLock, lockRelease);
    }

    [TestMethod]
    public void Dispose_DisposesLock()
    {
        ConfigureServiceProvider(out var serviceProvider, out _);
        ConfigureAsyncLockFactory(out var lockFactory, out var asyncLock, out _);

        var testSubject = CreateTestSubject(serviceProvider.Object, Mock.Of<IThreadHandling>(), lockFactory.Object);

        testSubject.Dispose();
        asyncLock.Verify(x => x.Dispose());
    }

    private static void VerifyThreadHandling(Mock<IThreadHandling> threadHandling)
    {
        threadHandling.Verify(x => x.ThrowIfOnUIThread());
        threadHandling.VerifyNoOtherCalls();
    }

    private static void VerifyServiceAddCall(Mock<IConfigurationScopeSLCoreService> configScopeService, ActiveConfigScopeTracker testSubject)
    {
        var currentConfigScopeDto = new ConfigurationScopeDto(testSubject.currentConfigScope.Id,
            testSubject.currentConfigScope.Id,
            true,
            testSubject.currentConfigScope.ConnectionId is not null ? new BindingConfigurationDto(testSubject.currentConfigScope.ConnectionId, testSubject.currentConfigScope.SonarProjectId) : null);
        configScopeService
            .Verify(x =>
                    x.DidAddConfigurationScopes(It.Is<DidAddConfigurationScopesParams>(p =>
                        p.addedScopes.SequenceEqual(new[] { currentConfigScopeDto }, new ConfigurationScopeDtoComparer()))),
                Times.Once);
        configScopeService.VerifyNoOtherCalls();
    }

    private static void VerifyServiceUpdateCall(Mock<IConfigurationScopeSLCoreService> configScopeService,
        ActiveConfigScopeTracker testSubject)
    {
        configScopeService
            .Verify(x => x.DidUpdateBinding(It.Is<DidUpdateBindingParams>(p =>
                p.configScopeId == testSubject.currentConfigScope.Id && new BindingConfigurationDtoComparer().Equals(p.updatedBinding, new BindingConfigurationDto(testSubject.currentConfigScope.ConnectionId, testSubject.currentConfigScope.SonarProjectId, true)))), Times.Once);
        configScopeService.VerifyNoOtherCalls();
    }

    private static void VerifyLockTakenSynchronouslyAndReleased(Mock<IAsyncLock> asyncLock, Mock<IReleaseAsyncLock> lockRelease)
    {
        asyncLock.Verify(x => x.Acquire(), Times.Once);
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
        IThreadHandling threadHandling,
        IAsyncLockFactory asyncLockFactory)
    {
        return new ActiveConfigScopeTracker(slCoreServiceProvider, asyncLockFactory, threadHandling);
    }

    private class ConfigurationScopeDtoComparer : IEqualityComparer<ConfigurationScopeDto>
    {
        public bool Equals(ConfigurationScopeDto x, ConfigurationScopeDto y)
        {
            if (x is null && y is null) { return true; }
            if (x is null || y is null) { return false; }

            return x.id == y.id && x.name == y.name && x.bindable == y.bindable && new BindingConfigurationDtoComparer().Equals(x.binding, y.binding);
        }

        public int GetHashCode(ConfigurationScopeDto obj)
        {
            return 0;
        }
    }

    private class BindingConfigurationDtoComparer : IEqualityComparer<BindingConfigurationDto>
    {
        public bool Equals(BindingConfigurationDto x, BindingConfigurationDto y)
        {
            if (x is null && y is null) { return true; }
            if (x is null || y is null) { return false; }

            return x.connectionId == y.connectionId && x.sonarProjectKey == y.sonarProjectKey && x.bindingSuggestionDisabled == y.bindingSuggestionDisabled;
        }

        public int GetHashCode(BindingConfigurationDto obj)
        {
            return 0;
        }
    }
}
