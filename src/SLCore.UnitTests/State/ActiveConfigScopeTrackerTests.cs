﻿/*
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
using SonarLint.VisualStudio.Core.ConfigurationScope;
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
    private ActiveConfigScopeTracker testSubject;
    private IConfigurationScopeSLCoreService configScopeService;
    private IAsyncLock asyncLock;
    private IReleaseAsyncLock lockRelease;
    private ISLCoreServiceProvider serviceProvider;
    private IAsyncLockFactory asyncLockFactory;
    private IThreadHandling threadHandling;
    private TestLogger logger;
    private EventHandler<ConfigurationScopeChangedEventArgs> currentConfigScopeChangedEventHandler;

    [TestInitialize]
    public void TestInitialize()
    {
        configScopeService = Substitute.For<IConfigurationScopeSLCoreService>();
        asyncLock = Substitute.For<IAsyncLock>();
        lockRelease = Substitute.For<IReleaseAsyncLock>();
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        threadHandling = Substitute.For<IThreadHandling>();
        ConfigureServiceProvider(isServiceAvailable:true);
        ConfigureAsyncLockFactory();
        currentConfigScopeChangedEventHandler = Substitute.For<EventHandler<ConfigurationScopeChangedEventArgs>>();
        logger = Substitute.ForPartsOf<TestLogger>();
        testSubject = new ActiveConfigScopeTracker(serviceProvider, asyncLockFactory, threadHandling, logger);
        testSubject.CurrentConfigurationScopeChanged += currentConfigScopeChangedEventHandler;
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ActiveConfigScopeTracker, IActiveConfigScopeTracker>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ActiveConfigScopeTracker>();
    }

    [TestMethod]
    public void Ctor_InitializesLogContexts() =>
        logger.Received(1).ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.ConfigurationScope_LogContext);

    [TestMethod]
    public void SetCurrentConfigScope_SetsUnboundScope()
    {
        const string configScopeId = "myid";

        testSubject.SetCurrentConfigScope(configScopeId);

        testSubject.CurrentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId));
        VerifyThreadHandling();
        VerifyServiceAddCall();
        VerifyLockTakenSynchronouslyAndReleased();
        VerifyCurrentConfigurationScopeChangedRaised(true);
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigScope_Declared, configScopeId));
    }

    [TestMethod]
    public void TryUpdateRootOnCurrentConfigScope_ConfigScopeSame_Updates()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const bool isReady = true;
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, IsReadyForAnalysis: isReady);
        const string root = "some root";
        const string baseDir = "some base dir";

        var result = testSubject.TryUpdateRootOnCurrentConfigScope(configScopeId, root, baseDir);

        result.Should().BeTrue();
        testSubject.CurrentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root, baseDir, isReady));
        VerifyCurrentConfigurationScopeChangedRaised(false);
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigScope_UpdatedFileSystem, configScopeId, root, baseDir));
    }

    [TestMethod]
    public void TryUpdateRootOnCurrentConfigScope_ConfigScopeDifferent_DoesNotUpdate()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const bool isReady = true;
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, IsReadyForAnalysis: isReady);

        var result = testSubject.TryUpdateRootOnCurrentConfigScope("some other id", "some root", "some base dir");

        result.Should().BeFalse();
        testSubject.CurrentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, IsReadyForAnalysis: isReady));
        VerifyCurrentConfigurationScopeChangedNotRaised();
    }

    [TestMethod]
    public void TryUpdateAnalysisReadinessOnCurrentConfigScope_ConfigScopeSame_Updates()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const string root = "root";
        const string baseDir = "basedir";
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root, baseDir);

        var result = testSubject.TryUpdateAnalysisReadinessOnCurrentConfigScope(configScopeId, true);

        result.Should().BeTrue();
        testSubject.CurrentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root, baseDir, true));
        VerifyCurrentConfigurationScopeChangedRaised(false);
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigScope_UpdatedAnalysisReadiness, configScopeId, true));
    }

    [TestMethod]
    public void TryUpdateAnalysisReadinessOnCurrentConfigScope_ConfigScopeDifferent_DoesNotUpdate()
    {
        const string configScopeId = "myid";
        const string connectionId = "connectionid";
        const string sonarProjectKey = "projectkey";
        const string root = "root";
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root);

        var result = testSubject.TryUpdateAnalysisReadinessOnCurrentConfigScope("some other id", true);

        result.Should().BeFalse();
        testSubject.CurrentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root));
        VerifyCurrentConfigurationScopeChangedNotRaised();
    }

    [TestMethod]
    public void SetCurrentConfigScope_SetsBoundScope()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";

        testSubject.SetCurrentConfigScope(configScopeId, connectionId, sonarProjectKey);

        testSubject.CurrentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey));
        VerifyThreadHandling();
        VerifyServiceAddCall();
        VerifyLockTakenSynchronouslyAndReleased();
        VerifyCurrentConfigurationScopeChangedRaised(true);
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigScope_Declared, configScopeId));

    }

    [TestMethod]
    public void SetCurrentConfigScope_CurrentScopeExists_UpdatesBoundScope()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        const string rootPath = "somepath";
        var existingConfigScope = new ConfigurationScope(configScopeId, RootPath: rootPath);
        testSubject.CurrentConfigScope = existingConfigScope;

        testSubject.SetCurrentConfigScope(configScopeId, connectionId, sonarProjectKey);

        testSubject.CurrentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, rootPath));
        testSubject.CurrentConfigScope.Should().NotBeSameAs(existingConfigScope);
        VerifyThreadHandling();
        VerifyServiceUpdateCall();
        VerifyLockTakenSynchronouslyAndReleased();
        VerifyCurrentConfigurationScopeChangedRaised(false);
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigScope_UpdatedBinding, configScopeId));
    }

    [TestMethod]
    public void SetCurrentConfigScope_ServiceUnavailable_Throws()
    {
        ConfigureServiceProvider(isServiceAvailable: false);

        var act = () => testSubject.SetCurrentConfigScope("id");

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
        VerifyThreadHandling();
        VerifyCurrentConfigurationScopeChangedNotRaised();
    }

    [TestMethod]
    public void SetCurrentConfigScope_UpdateConfigScopeWithDifferentId_Throws()
    {
        const string configScopeId = "myid";
        const string anotherConfigScopeId = "anotherid";
        var existingConfigScope = new ConfigurationScope(configScopeId);
        testSubject.CurrentConfigScope = existingConfigScope;

        var act = () => testSubject.SetCurrentConfigScope(anotherConfigScopeId);

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ConfigScopeConflict);
        VerifyThreadHandling();
        VerifyCurrentConfigurationScopeChangedNotRaised();
    }

    [TestMethod]
    public void RemoveCurrentConfigScope_RemovesScope()
    {
        const string configScopeId = "myid";
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId);

        testSubject.RemoveCurrentConfigScope();

        configScopeService.Received().DidRemoveConfigurationScope(Arg.Is<DidRemoveConfigurationScopeParams>(p => p.removedId == configScopeId));
        VerifyThreadHandling();
        VerifyLockTakenSynchronouslyAndReleased();
        VerifyCurrentConfigurationScopeChangedRaised(true);
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigScope_Removed, configScopeId));
    }

    [TestMethod]
    public void RemoveCurrentConfigScope_NoCurrentScope_DoesNothing()
    {
        testSubject.RemoveCurrentConfigScope();

        configScopeService.ReceivedCalls().Count().Should().Be(0);
        VerifyThreadHandling();
        VerifyLockTakenSynchronouslyAndReleased();
        VerifyCurrentConfigurationScopeChangedNotRaised();
    }

    [TestMethod]
    public void RemoveCurrentConfigScope_ServiceUnavailable_Throws()
    {
        testSubject.CurrentConfigScope = new ConfigurationScope("some Id", default, default, default);
        ConfigureServiceProvider(isServiceAvailable: false);

        var act = () => testSubject.RemoveCurrentConfigScope();

        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
        VerifyThreadHandling();
        VerifyCurrentConfigurationScopeChangedNotRaised();
    }

    [TestMethod]
    public void GetCurrent_ReturnsUnboundScope()
    {
        const string configScopeId = "myid";
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId);

        var currentScope = testSubject.Current;

        currentScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId));
        VerifyThreadHandling();
        VerifyLockTakenSynchronouslyAndReleased();
    }

    [TestMethod]
    public void GetCurrent_ReturnsBoundScope()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey);

        var currentScope = testSubject.Current;

        currentScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey));
        currentScope.RootPath.Should().Be(null); // not implemented
        VerifyThreadHandling();
        VerifyLockTakenSynchronouslyAndReleased();
    }

    [TestMethod]
    public void Reset_SetsCurrentScopeToNull()
    {
        const string configScopeId = "myid";
        const string connectionId = "myconid";
        const string sonarProjectKey = "projectkey";
        testSubject.CurrentConfigScope = new ConfigurationScope(configScopeId, configScopeId, connectionId, sonarProjectKey);

        testSubject.Reset();

        testSubject.CurrentConfigScope.Should().BeNull();
        serviceProvider.ReceivedCalls().Count().Should().Be(0);
        VerifyThreadHandling();
        VerifyLockTakenSynchronouslyAndReleased();
        VerifyCurrentConfigurationScopeChangedRaised(true);
        logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigScope_Reset));
    }

    [TestMethod]
    public void Dispose_DisposesLock()
    {
        testSubject.Dispose();

        asyncLock.Received(1).Dispose();
    }

    private void VerifyThreadHandling()
    {
        threadHandling.Received(1).ThrowIfOnUIThread();
        threadHandling.ReceivedCalls().Count().Should().Be(1); // verify no other calls
    }

    private void VerifyServiceAddCall()
    {
        var currentConfigScopeDto = new ConfigurationScopeDto(testSubject.CurrentConfigScope.Id,
            testSubject.CurrentConfigScope.Id,
            true,
            testSubject.CurrentConfigScope.ConnectionId is not null ? new BindingConfigurationDto(testSubject.CurrentConfigScope.ConnectionId, testSubject.CurrentConfigScope.SonarProjectId) : null);
        configScopeService.Received(1).DidAddConfigurationScopes(Arg.Is<DidAddConfigurationScopesParams>(p =>
                        p.addedScopes.SequenceEqual(new[] { currentConfigScopeDto }, new ConfigurationScopeDtoComparer())));
        configScopeService.ReceivedCalls().Count().Should().Be(1);
    }

    private void VerifyServiceUpdateCall()
    {
        configScopeService.Received(1).DidUpdateBinding(Arg.Is<DidUpdateBindingParams>(p =>
                p.configScopeId == testSubject.CurrentConfigScope.Id && new BindingConfigurationDtoComparer().Equals(p.updatedBinding, new BindingConfigurationDto(testSubject.CurrentConfigScope.ConnectionId, testSubject.CurrentConfigScope.SonarProjectId, true))));
        configScopeService.ReceivedCalls().Count().Should().Be(1);
    }

    private void VerifyLockTakenSynchronouslyAndReleased()
    {
        asyncLock.Received(1).Acquire();
        lockRelease.Received(1).Dispose();
    }

    private void ConfigureAsyncLockFactory()
    {
        asyncLock.AcquireAsync().Returns(lockRelease);
        asyncLock.Acquire().Returns(lockRelease);
        asyncLockFactory.Create().Returns(asyncLock);
    }

    private void ConfigureServiceProvider(bool isServiceAvailable)
    {
        serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService _).Returns(x =>
        {
            x[0] = configScopeService;
            return isServiceAvailable;
        });
    }

    private void VerifyCurrentConfigurationScopeChangedRaised(bool definitionChanged)
    {
        currentConfigScopeChangedEventHandler.Received(1).Invoke(testSubject, Arg.Is<ConfigurationScopeChangedEventArgs>(x => x.DefinitionChanged == definitionChanged));
    }

    private void VerifyCurrentConfigurationScopeChangedNotRaised()
    {
        currentConfigScopeChangedEventHandler.DidNotReceive().Invoke(testSubject, Arg.Any<ConfigurationScopeChangedEventArgs>());
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
