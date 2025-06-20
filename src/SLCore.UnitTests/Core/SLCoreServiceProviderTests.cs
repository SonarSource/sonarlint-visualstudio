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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Core;

[TestClass]
public class SLCoreServiceProviderTests
{
    private InitializeParams defaultInitializeParams = new(default, default, default, default, default, default, default, default, default, default, default, default, default, default, default,
        default, default, default);
    private ISLCoreJsonRpc rpcMock;

    [TestInitialize]
    public void TestInitialize()
    {
        rpcMock = Substitute.For<ISLCoreJsonRpc>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreServiceProvider, ISLCoreServiceProvider>(
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_WriterInterface_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreServiceProvider, ISLCoreServiceProviderWriter>(
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreServiceProvider>();
    }

    [TestMethod]
    public void TryGetTransientService_TypeNotInterface_Throws()
    {
        var testSubject = CreateTestSubject();

        Action act = () => testSubject.TryGetTransientService(out TestSLCoreService _);

        act.Should().Throw<ArgumentException>().WithMessage($"The type argument {typeof(TestSLCoreService).FullName} is not an interface");
    }

    [TestMethod]
    public void TryGetTransientService_NotUIThread_Checked()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        var testSubject = CreateTestSubject(threadHandling: threadHandling);

        testSubject.TryGetTransientService(out ITestSLcoreService1 _);

        threadHandling.Received(1).ThrowIfOnUIThread();
    }

    [TestMethod]
    public void TryGetTransientService_NotInitialized_ReturnsFalse()
    {
        var testSubject = CreateTestSubject();

        testSubject.TryGetTransientService(out ITestSLcoreService1 _).Should().BeFalse();
    }

    [TestMethod]
    public void TryGetTransientService_ConnectionDied_ReturnsFalse()
    {
        SetUpConnectionState(rpcMock, false);

        var testSubject = CreateTestSubject(rpcMock);

        testSubject.TryGetTransientService(out ITestSLcoreService1 _).Should().BeFalse();
    }

    [TestMethod]
    public void TryGetTransientService_RpcThrows_LoggedAndReturnsFalse()
    {
        SetUpConnectionState(rpcMock, true);
        var logger = new TestLogger();
        var testSubject = CreateTestSubject(rpcMock, true, logger: logger);
        rpcMock.CreateService<ISLCoreService>().Throws(new Exception("Service is not Created"));

        var result = testSubject.TryGetTransientService(out ISLCoreService service);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists("Service is not Created");
    }

    [TestMethod]
    public void TryGetTransientService_ConnectionIsAlive_ReturnsTrueAndCreatesService()
    {
        SetUpConnectionState(rpcMock, true);
        var testSubject = CreateTestSubject(rpcMock, true);
        var service1 = Substitute.For<ITestSLcoreService1>();
        SetUpServiceCreation(rpcMock, service1);

        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService).Should().BeTrue();

        requestedService.Should().BeSameAs(service1);
        rpcMock.Received(1).CreateService<ITestSLcoreService1>();
    }

    [TestMethod]
    public void TryGetTransientService_ServiceAlreadyCreated_ReturnsTrueAndCachedCopy()
    {
        SetUpConnectionState(rpcMock, true);
        var testSubject = CreateTestSubject(rpcMock, true);
        var service1 = Substitute.For<ITestSLcoreService1>();
        SetUpServiceCreation(rpcMock, service1);

        testSubject.TryGetTransientService(out ITestSLcoreService1 _);

        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService).Should().BeTrue();

        requestedService.Should().BeSameAs(service1);
        rpcMock.Received(1).CreateService<ITestSLcoreService1>();
    }

    [TestMethod]
    public void TryGetTransientService_ConnectionReset_ReturnsTrueAndCreatesService()
    {
        var service1 = Substitute.For<ITestSLcoreService1>();
        SetUpConnectionState(rpcMock, true);
        var testSubject = CreateTestSubject(rpcMock, true);
        SetUpServiceCreation(rpcMock, service1);

        testSubject.TryGetTransientService(out ITestSLcoreService1 _); //caching
        var service2 = Substitute.For<ITestSLcoreService1>();
        var rpcMock2 = Substitute.For<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock2, true);
        SetUpServiceCreation(rpcMock2, service2);

        testSubject.SetCurrentConnection(rpcMock2);
        SetInitializedState(rpcMock2, testSubject);
        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService).Should().BeTrue();

        requestedService.Should().BeSameAs(service2);
        rpcMock.Received(1).CreateService<ITestSLcoreService1>();
        rpcMock2.Received(1).CreateService<ITestSLcoreService1>();
    }

    [TestMethod]
    public void SetCurrentConnection_ClearsAllCachedServices()
    {
        var service1 = Substitute.For<ITestSLcoreService1>();
        var service2 = Substitute.For<ITestSLcoreService2>();
        var service3 = Substitute.For<ITestSLcoreService3>();

        SetUpConnectionState(rpcMock, true);
        var testSubject = CreateTestSubject(rpcMock, true);
        SetUpServiceCreation(rpcMock, service1);
        SetUpServiceCreation(rpcMock, service2);
        SetUpServiceCreation(rpcMock, service3);

        testSubject.TryGetTransientService(out ITestSLcoreService1 _).Should().BeTrue();
        testSubject.TryGetTransientService(out ITestSLcoreService2 _).Should().BeTrue();
        testSubject.TryGetTransientService(out ITestSLcoreService3 _).Should().BeTrue();

        var service1New = Substitute.For<ITestSLcoreService1>();
        var service2New = Substitute.For<ITestSLcoreService2>();
        var service3New = Substitute.For<ITestSLcoreService3>();
        var rpcMock2 = Substitute.For<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock2, true);
        SetUpServiceCreation(rpcMock2, service1New);
        SetUpServiceCreation(rpcMock2, service2New);
        SetUpServiceCreation(rpcMock2, service3New);

        testSubject.SetCurrentConnection(rpcMock2);
        SetInitializedState(rpcMock2, testSubject);

        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService1).Should().BeTrue();
        requestedService1.Should().BeSameAs(service1New).And.NotBeSameAs(service1);
        testSubject.TryGetTransientService(out ITestSLcoreService2 requestedService2).Should().BeTrue();
        requestedService2.Should().BeSameAs(service2New).And.NotBeSameAs(service2);
        testSubject.TryGetTransientService(out ITestSLcoreService3 requestedService3).Should().BeTrue();
        requestedService3.Should().BeSameAs(service3New).And.NotBeSameAs(service3);
    }

    [TestMethod]
    public void TryGetTransientService_WithoutInitialize_ReturnsFalse()
    {
        var testSubject = CreateTestSubject(rpcMock);

        var result = testSubject.TryGetTransientService(out ITestSLcoreService1? service);

        result.Should().BeFalse();
        service.Should().BeNull();
    }

    [TestMethod]
    public void TryGetTransientService_WithoutInitialize_AndConnectionAlive_ReturnsFalse()
    {
        SetUpConnectionState(rpcMock, true);
        var testSubject = CreateTestSubject(rpcMock);

        var result = testSubject.TryGetTransientService(out ITestSLcoreService1? service);

        result.Should().BeFalse();
        service.Should().BeNull();
    }

    [TestMethod]
    public void TryGetTransientService_WithoutInitialize_AndConnectionNotAlive_ReturnsFalse()
    {
        SetUpConnectionState(rpcMock, false);
        var testSubject = CreateTestSubject(rpcMock);

        var result = testSubject.TryGetTransientService(out ITestSLcoreService1 service);

        result.Should().BeFalse();
        service.Should().BeNull();
    }

    [TestMethod]
    public void SetCurrentConnection_ResetsInitializationState()
    {
        SetUpConnectionState(rpcMock, true);
        var testSubject = CreateTestSubject(rpcMock, initialized: true);
        var service1 = Substitute.For<ITestSLcoreService1>();
        SetUpServiceCreation(rpcMock, service1);

        testSubject.SetCurrentConnection(rpcMock);

        testSubject.TryGetTransientService(out ITestSLcoreService1 serviceAfter).Should().BeFalse();
        serviceAfter.Should().BeNull();
    }

    [TestMethod]
    public void Initialize_Successful_CallsLifecycleServiceAndSetsInitialized()
    {
        SetUpConnectionState(rpcMock, true);
        var lifecycleService = Substitute.For<ILifecycleManagementSLCoreService>();
        rpcMock.CreateService<ILifecycleManagementSLCoreService>().Returns(lifecycleService);
        var testSubject = CreateTestSubject(rpcMock);

        testSubject.Initialize(defaultInitializeParams);

        testSubject.IsInitialized.Should().BeTrue();
        lifecycleService.Received(1).Initialize(defaultInitializeParams);
    }

    [TestMethod]
    public void Initialize_ThrowsIfAlreadyInitialized()
    {
        SetUpConnectionState(rpcMock, true);
        var testSubject = CreateTestSubject(rpcMock, initialized: true);
        var lifecycleService = Substitute.For<ILifecycleManagementSLCoreService>();
        rpcMock.CreateService<ILifecycleManagementSLCoreService>().Returns(lifecycleService);

        Action act = () => testSubject.Initialize(defaultInitializeParams);

        testSubject.IsInitialized.Should().BeTrue();
        act.Should().Throw<InvalidOperationException>().WithMessage(SLCoreStrings.BackendAlreadyInitialized);
    }

    [TestMethod]
    public void Initialize_ThrowsIfNoConnection()
    {
        var testSubject = CreateTestSubject();

        Action act = () => testSubject.Initialize(defaultInitializeParams);

        testSubject.IsInitialized.Should().BeFalse();
        act.Should().Throw<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void Initialize_ThrowsIfConnectionNotAlive()
    {
        SetUpConnectionState(rpcMock, false);
        var testSubject = CreateTestSubject(rpcMock);

        Action act = () => testSubject.Initialize(defaultInitializeParams);

        testSubject.IsInitialized.Should().BeFalse();
        act.Should().Throw<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void Initialize_ThrowsIfLifecycleServiceCannotBeCreated()
    {
        SetUpConnectionState(rpcMock, true);
        rpcMock.CreateService<ILifecycleManagementSLCoreService>().Throws(new Exception("creation failed"));
        var testSubject = CreateTestSubject(rpcMock);

        Action act = () => testSubject.Initialize(defaultInitializeParams);

        testSubject.IsInitialized.Should().BeFalse();
        act.Should().Throw<InvalidOperationException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
    }

    private static void SetUpServiceCreation<T>(ISLCoreJsonRpc rpcMock, T service) where T : class, ISLCoreService
    {
        rpcMock.CreateService<T>().Returns(service);
    }

    private static void SetUpConnectionState(ISLCoreJsonRpc rpcMock, bool isAlive)
    {
        rpcMock.IsAlive.Returns(isAlive);
    }

    private SLCoreServiceProvider CreateTestSubject(
        ISLCoreJsonRpc jsonRpc = null,
        bool initialized = false,
        IThreadHandling threadHandling = null,
        ILogger logger = null)
    {
        threadHandling ??= new NoOpThreadHandler();
        logger ??= new TestLogger();
        var testSubject = new SLCoreServiceProvider(threadHandling, logger);
        if (jsonRpc != null)
        {
            testSubject.SetCurrentConnection(jsonRpc);
            if (initialized)
            {
                SetInitializedState(jsonRpc, testSubject);
            }
        }
        return testSubject;
    }

    private void SetInitializedState(ISLCoreJsonRpc jsonRpc, SLCoreServiceProvider testSubject)
    {
        var lifecycleManagementSlCoreService = Substitute.For<ILifecycleManagementSLCoreService>();
        jsonRpc.CreateService<ILifecycleManagementSLCoreService>().Returns(lifecycleManagementSlCoreService);
        testSubject.Initialize(defaultInitializeParams);
        lifecycleManagementSlCoreService.Received(1).Initialize(defaultInitializeParams);
    }

    public class TestSLCoreService : ISLCoreService
    {
    }

    public interface ITestSLcoreService1 : ISLCoreService
    {
    }

    public interface ITestSLcoreService2 : ISLCoreService
    {
    }

    public interface ITestSLcoreService3 : ISLCoreService
    {
    }
}
