///*
// * SonarLint for Visual Studio
// * Copyright (C) 2016-2024 SonarSource SA
// * mailto:info AT sonarsource DOT com
// *
// * This program is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 3 of the License, or (at your option) any later version.
// *
// * This program is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public License
// * along with this program; if not, write to the Free Software Foundation,
// * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// */

//using SonarLint.VisualStudio.Core;
//using SonarLint.VisualStudio.Core.Binding;
//using SonarLint.VisualStudio.Core.Synchronization;
//using SonarLint.VisualStudio.SLCore.Core;
//using SonarLint.VisualStudio.SLCore.Service.Project;
//using SonarLint.VisualStudio.SLCore.Service.Project.Models;
//using SonarLint.VisualStudio.SLCore.Service.Project.Params;
//using SonarLint.VisualStudio.SLCore.State;

//namespace SonarLint.VisualStudio.SLCore.UnitTests.State;

//[TestClass]
//public class ActiveConfigScopeTrackerTests
//{
//    private ActiveConfigScopeTracker testSubject;
//    private IConfigurationScopeSLCoreService configScopeService;
//    private IAsyncLock asyncLock;
//    private IReleaseAsyncLock lockRelease;
//    private ISLCoreServiceProvider serviceProvider;
//    private IAsyncLockFactory asyncLockFactory;
//    private IThreadHandling threadHandling;
//    private EventHandler currentConfigScopeChangedEventHandler;

//    [TestInitialize]
//    public void TestInitialize()
//    {
//        configScopeService = Substitute.For<IConfigurationScopeSLCoreService>();
//        asyncLock = Substitute.For<IAsyncLock>();
//        lockRelease = Substitute.For<IReleaseAsyncLock>();
//        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
//        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
//        threadHandling = Substitute.For<IThreadHandling>();
//        ConfigureServiceProvider(isServiceAvailable:true);
//        ConfigureAsyncLockFactory();
//        currentConfigScopeChangedEventHandler = Substitute.For<EventHandler>();

//        testSubject = new ActiveConfigScopeTracker(serviceProvider, asyncLockFactory, threadHandling);
//        testSubject.CurrentConfigurationScopeChanged += currentConfigScopeChangedEventHandler;
//    }

//    [TestMethod]
//    public void MefCtor_CheckIsExported()
//    {
//        MefTestHelpers.CheckTypeCanBeImported<ActiveConfigScopeTracker, IActiveConfigScopeTracker>(
//            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
//            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
//            MefTestHelpers.CreateExport<IThreadHandling>());
//    }

//    [TestMethod]
//    public void MefCtor_CheckIsSingleton()
//    {
//        MefTestHelpers.CheckIsSingletonMefComponent<ActiveConfigScopeTracker>();
//    }

//    [TestMethod]
//    public void SetCurrentConfigScope_SetsUnboundScope()
//    {
//        const string configScopeId = "myid";

//        testSubject.SetCurrentConfigScope(configScopeId);

//        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId));
//        VerifyThreadHandling();
//        VerifyServiceAddCall();
//        VerifyLockTakenSynchronouslyAndReleased();
//        VerifyCurrentConfigurationScopeChangedRaised();
//    }

//    [TestMethod]
//    public void TryUpdateRootOnCurrentConfigScope_ConfigScopeSame_Updates()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "connectionid";
//        const string sonarProjectKey = "projectkey";
//        const bool isReady = true;
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, IsReadyForAnalysis: isReady);

//        var result = testSubject.TryUpdateRootOnCurrentConfigScope(configScopeId, "some root");
        
//        result.Should().BeTrue();
//        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, "some root", isReady));
//        VerifyCurrentConfigurationScopeChangedRaised();
//    }

//    [TestMethod]
//    public void TryUpdateRootOnCurrentConfigScope_ConfigScopeDifferent_DoesNotUpdate()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "connectionid";
//        const string sonarProjectKey = "projectkey";
//        const bool isReady = true;
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, IsReadyForAnalysis: isReady);

//        var result = testSubject.TryUpdateRootOnCurrentConfigScope("some other id", "some root");
        
//        result.Should().BeFalse();
//        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, IsReadyForAnalysis: isReady));
//        VerifyCurrentConfigurationScopeChangedNotRaised();
//    }
    
//    [TestMethod]
//    public void TryUpdateAnalysisReadinessOnCurrentConfigScope_ConfigScopeSame_Updates()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "connectionid";
//        const string sonarProjectKey = "projectkey";
//        const string root = "root";
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root);

//        var result = testSubject.TryUpdateAnalysisReadinessOnCurrentConfigScope(configScopeId, true);
        
//        result.Should().BeTrue();
//        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root, true));
//        VerifyCurrentConfigurationScopeChangedRaised();
//    }

//    [TestMethod]
//    public void TryUpdateAnalysisReadinessOnCurrentConfigScope_ConfigScopeDifferent_DoesNotUpdate()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "connectionid";
//        const string sonarProjectKey = "projectkey";
//        const string root = "root";
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root);

//        var result = testSubject.TryUpdateAnalysisReadinessOnCurrentConfigScope("some other id", true);
        
//        result.Should().BeFalse();
//        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, root));
//        VerifyCurrentConfigurationScopeChangedNotRaised();
//    }

//    [TestMethod]
//    public void SetCurrentConfigScope_SetsBoundScope()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "myconid";
//        const string sonarProjectKey = "projectkey";

//        testSubject.SetCurrentConfigScope(configScopeId, connectionId, sonarProjectKey);

//        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey));
//        VerifyThreadHandling();
//        VerifyServiceAddCall();
//        VerifyLockTakenSynchronouslyAndReleased();
//        VerifyCurrentConfigurationScopeChangedRaised();
//    }

//    [TestMethod]
//    public void SetCurrentConfigScope_CurrentScopeExists_UpdatesBoundScope()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "myconid";
//        const string sonarProjectKey = "projectkey";
//        const string rootPath = "somepath";
//        var existingConfigScope = new ConfigurationScope(configScopeId, RootPath: rootPath);
//        testSubject.currentConfigScope = existingConfigScope;

//        testSubject.SetCurrentConfigScope(configScopeId, connectionId, sonarProjectKey);

//        testSubject.currentConfigScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey, rootPath));
//        testSubject.currentConfigScope.Should().NotBeSameAs(existingConfigScope);
//        VerifyThreadHandling();
//        VerifyServiceUpdateCall();
//        VerifyLockTakenSynchronouslyAndReleased();
//        VerifyCurrentConfigurationScopeChangedRaised();
//    }

//    [TestMethod]
//    public void SetCurrentConfigScope_ServiceUnavailable_Throws()
//    {
//        ConfigureServiceProvider(isServiceAvailable: false);

//        var act = () => testSubject.SetCurrentConfigScope("id");

//        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
//        VerifyThreadHandling();
//        VerifyCurrentConfigurationScopeChangedNotRaised();
//    }

//    [TestMethod]
//    public void SetCurrentConfigScope_UpdateConfigScopeWithDifferentId_Throws()
//    {
//        const string configScopeId = "myid";
//        const string anotherConfigScopeId = "anotherid";
//        var existingConfigScope = new ConfigurationScope(configScopeId);
//        testSubject.currentConfigScope = existingConfigScope;

//        var act = () => testSubject.SetCurrentConfigScope(anotherConfigScopeId);

//        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ConfigScopeConflict);
//        VerifyThreadHandling();
//        VerifyCurrentConfigurationScopeChangedNotRaised();
//    }

//    [TestMethod]
//    public void RemoveCurrentConfigScope_RemovesScope()
//    {
//        const string configScopeId = "myid";
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId);

//        testSubject.RemoveCurrentConfigScope();

//        configScopeService.Received().DidRemoveConfigurationScope(Arg.Is<DidRemoveConfigurationScopeParams>(p => p.removedId == configScopeId));
//        VerifyThreadHandling();
//        VerifyLockTakenSynchronouslyAndReleased();
//        VerifyCurrentConfigurationScopeChangedRaised();
//    }

//    [TestMethod]
//    public void RemoveCurrentConfigScope_NoCurrentScope_DoesNothing()
//    {
//        testSubject.RemoveCurrentConfigScope();

//        configScopeService.ReceivedCalls().Count().Should().Be(0);
//        VerifyThreadHandling();
//        VerifyLockTakenSynchronouslyAndReleased();
//        VerifyCurrentConfigurationScopeChangedNotRaised();
//    }

//    [TestMethod]
//    public void RemoveCurrentConfigScope_ServiceUnavailable_Throws()
//    {
//        testSubject.currentConfigScope = new ConfigurationScope("some Id", default, default, default);
//        ConfigureServiceProvider(isServiceAvailable: false);

//        var act = () => testSubject.RemoveCurrentConfigScope();

//        act.Should().ThrowExactly<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
//        VerifyThreadHandling();
//        VerifyCurrentConfigurationScopeChangedNotRaised();
//    }

//    [TestMethod]
//    public void GetCurrent_ReturnsUnboundScope()
//    {
//        const string configScopeId = "myid";
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId);

//        var currentScope = testSubject.Current;

//        currentScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId));
//        VerifyThreadHandling();
//        VerifyLockTakenSynchronouslyAndReleased();
//    }

//    [TestMethod]
//    public void GetCurrent_ReturnsBoundScope()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "myconid";
//        const string sonarProjectKey = "projectkey";
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, connectionId, sonarProjectKey);

//        var currentScope = testSubject.Current;

//        currentScope.Should().BeEquivalentTo(new ConfigurationScope(configScopeId, connectionId, sonarProjectKey));
//        currentScope.RootPath.Should().Be(null); // not implemented
//        VerifyThreadHandling();
//        VerifyLockTakenSynchronouslyAndReleased();
//    }

//    [TestMethod]
//    public void Reset_SetsCurrentScopeToNull()
//    {
//        const string configScopeId = "myid";
//        const string connectionId = "myconid";
//        const string sonarProjectKey = "projectkey";
//        testSubject.currentConfigScope = new ConfigurationScope(configScopeId, configScopeId, connectionId, sonarProjectKey);

//        testSubject.Reset();

//        testSubject.currentConfigScope.Should().BeNull();
//        serviceProvider.ReceivedCalls().Count().Should().Be(0);
//        VerifyThreadHandling();
//        VerifyLockTakenSynchronouslyAndReleased();
//        VerifyCurrentConfigurationScopeChangedRaised();
//    }

//    [TestMethod]
//    public void Dispose_DisposesLock()
//    {
//        testSubject.Dispose();

//        asyncLock.Received(1).Dispose();
//    }

//    private void VerifyThreadHandling()
//    {
//        threadHandling.Received(1).ThrowIfOnUIThread();
//        threadHandling.ReceivedCalls().Count().Should().Be(1); // verify no other calls
//    }

//    private void VerifyServiceAddCall()
//    {
//        var currentConfigScopeDto = new ConfigurationScopeDto(testSubject.currentConfigScope.Id,
//            testSubject.currentConfigScope.Id,
//            true,
//            testSubject.currentConfigScope.ConnectionId is not null ? new BindingConfigurationDto(testSubject.currentConfigScope.ConnectionId, testSubject.currentConfigScope.SonarProjectId) : null);
//        configScopeService.Received(1).DidAddConfigurationScopes(Arg.Is<DidAddConfigurationScopesParams>(p =>
//                        p.addedScopes.SequenceEqual(new[] { currentConfigScopeDto }, new ConfigurationScopeDtoComparer())));
//        configScopeService.ReceivedCalls().Count().Should().Be(1);
//    }

//    private void VerifyServiceUpdateCall()
//    {
//        configScopeService.Received(1).DidUpdateBinding(Arg.Is<DidUpdateBindingParams>(p =>
//                p.configScopeId == testSubject.currentConfigScope.Id && new BindingConfigurationDtoComparer().Equals(p.updatedBinding, new BindingConfigurationDto(testSubject.currentConfigScope.ConnectionId, testSubject.currentConfigScope.SonarProjectId, true))));
//        configScopeService.ReceivedCalls().Count().Should().Be(1);
//    }

//    private void VerifyLockTakenSynchronouslyAndReleased()
//    {
//        asyncLock.Received(1).Acquire();
//        lockRelease.Received(1).Dispose();
//    }

//    private void ConfigureAsyncLockFactory()
//    {
//        asyncLock.AcquireAsync().Returns(lockRelease);
//        asyncLock.Acquire().Returns(lockRelease);
//        asyncLockFactory.Create().Returns(asyncLock);
//    }

//    private void ConfigureServiceProvider(bool isServiceAvailable)
//    {
//        serviceProvider.TryGetTransientService(out IConfigurationScopeSLCoreService _).Returns(x =>
//        {
//            x[0] = configScopeService;
//            return isServiceAvailable;
//        });
//    }

//    private void VerifyCurrentConfigurationScopeChangedRaised()
//    {
//        currentConfigScopeChangedEventHandler.Received(1).Invoke(testSubject, Arg.Any<EventArgs>());
//    }

//    private void VerifyCurrentConfigurationScopeChangedNotRaised()
//    {
//        currentConfigScopeChangedEventHandler.DidNotReceive().Invoke(testSubject, Arg.Any<EventArgs>());
//    }

//    private class ConfigurationScopeDtoComparer : IEqualityComparer<ConfigurationScopeDto>
//    {
//        public bool Equals(ConfigurationScopeDto x, ConfigurationScopeDto y)
//        {
//            if (x is null && y is null) { return true; }
//            if (x is null || y is null) { return false; }

//            return x.id == y.id && x.name == y.name && x.bindable == y.bindable && new BindingConfigurationDtoComparer().Equals(x.binding, y.binding);
//        }

//        public int GetHashCode(ConfigurationScopeDto obj)
//        {
//            return 0;
//        }
//    }

//    private class BindingConfigurationDtoComparer : IEqualityComparer<BindingConfigurationDto>
//    {
//        public bool Equals(BindingConfigurationDto x, BindingConfigurationDto y)
//        {
//            if (x is null && y is null) { return true; }
//            if (x is null || y is null) { return false; }

//            return x.connectionId == y.connectionId && x.sonarProjectKey == y.sonarProjectKey && x.bindingSuggestionDisabled == y.bindingSuggestionDisabled;
//        }

//        public int GetHashCode(BindingConfigurationDto obj)
//        {
//            return 0;
//        }
//    }
//}
