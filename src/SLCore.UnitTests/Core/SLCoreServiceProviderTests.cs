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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Core;

[TestClass]
public class SLCoreServiceProviderTests
{
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
        var threadHandling = new Mock<IThreadHandling>();
        var testSubject = CreateTestSubject(threadHandling:threadHandling.Object);

        testSubject.TryGetTransientService(out ITestSLcoreService1 _);
        
        threadHandling.Verify(x => x.ThrowIfOnUIThread());
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
        var rpcMock = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock, false);

        var testSubject = CreateTestSubject(rpcMock.Object);

        testSubject.TryGetTransientService(out ITestSLcoreService1 _).Should().BeFalse();
    }

    [TestMethod]
    public void TryGetTransientService_RpcThrows_LoggedAndReturnsFalse()
    {
        var rpcMock = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock, true);
        rpcMock.Setup(r => r.CreateService<ISLCoreService>()).Throws(new Exception("Service is not Created"));

        var logger = new TestLogger();

        var testSubject = CreateTestSubject(rpcMock.Object, logger:logger);

        var result = testSubject.TryGetTransientService(out ISLCoreService service);

        result.Should().BeFalse();
        logger.AssertPartialOutputStringExists("Service is not Created");
    }

    [TestMethod]
    public void TryGetTransientService_ConnectionIsAlive_ReturnsTrueAndCreatesService()
    {
        var rpcMock = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock, true);
        var service1 = Mock.Of<ITestSLcoreService1>();
        SetUpServiceCreation(rpcMock, service1);

        var testSubject = CreateTestSubject(rpcMock.Object);

        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService).Should().BeTrue();

        requestedService.Should().BeSameAs(service1);
        rpcMock.Verify(x => x.CreateService<ITestSLcoreService1>(), Times.Once);
    }

    [TestMethod]
    public void TryGetTransientService_ServiceAlreadyCreated_ReturnsTrueAndCachedCopy()
    {
        var rpcMock = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock, true);
        var service1 = Mock.Of<ITestSLcoreService1>();
        SetUpServiceCreation(rpcMock, service1);

        var testSubject = CreateTestSubject(rpcMock.Object);
        testSubject.TryGetTransientService(out ITestSLcoreService1 _);

        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService).Should().BeTrue();

        requestedService.Should().BeSameAs(service1);
        rpcMock.Verify(x => x.CreateService<ITestSLcoreService1>(), Times.Once);
    }

    [TestMethod]
    public void TryGetTransientService_ConnectionReset_ReturnsTrueAndCreatesService()
    {
        var service1 = Mock.Of<ITestSLcoreService1>();
        var rpcMock1 = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock1, true);
        SetUpServiceCreation(rpcMock1, service1);

        var testSubject = CreateTestSubject(rpcMock1.Object);
        testSubject.TryGetTransientService(out ITestSLcoreService1 _); //caching
        var service2 = Mock.Of<ITestSLcoreService1>();
        var rpcMock2 = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock2, true);
        SetUpServiceCreation(rpcMock2, service2);

        testSubject.SetCurrentConnection(rpcMock2.Object);
        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService).Should().BeTrue();

        requestedService.Should().BeSameAs(service2);
        rpcMock1.Verify(x => x.CreateService<ITestSLcoreService1>(), Times.Once);
        rpcMock2.Verify(x => x.CreateService<ITestSLcoreService1>(), Times.Once);
    }

    [TestMethod]
    public void SetCurrentConnection_ClearsAllCachedServices()
    {
        var service1 = Mock.Of<ITestSLcoreService1>();
        var service2 = Mock.Of<ITestSLcoreService2>();
        var service3 = Mock.Of<ITestSLcoreService3>();
        var rpcMock1 = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock1, true);
        SetUpServiceCreation(rpcMock1, service1);
        SetUpServiceCreation(rpcMock1, service2);
        SetUpServiceCreation(rpcMock1, service3);

        var testSubject = CreateTestSubject(rpcMock1.Object);
        testSubject.TryGetTransientService(out ITestSLcoreService1 _).Should().BeTrue();
        testSubject.TryGetTransientService(out ITestSLcoreService2 _).Should().BeTrue();
        testSubject.TryGetTransientService(out ITestSLcoreService3 _).Should().BeTrue();

        var service1New = Mock.Of<ITestSLcoreService1>();
        var service2New = Mock.Of<ITestSLcoreService2>();
        var service3New = Mock.Of<ITestSLcoreService3>();
        var rpcMock2 = new Mock<ISLCoreJsonRpc>();
        SetUpConnectionState(rpcMock2, true);
        SetUpServiceCreation(rpcMock2, service1New);
        SetUpServiceCreation(rpcMock2, service2New);
        SetUpServiceCreation(rpcMock2, service3New);

        testSubject.SetCurrentConnection(rpcMock2.Object);

        testSubject.TryGetTransientService(out ITestSLcoreService1 requestedService1).Should().BeTrue();
        requestedService1.Should().BeSameAs(service1New).And.NotBeSameAs(service1);
        testSubject.TryGetTransientService(out ITestSLcoreService2 requestedService2).Should().BeTrue();
        requestedService2.Should().BeSameAs(service2New).And.NotBeSameAs(service2);
        testSubject.TryGetTransientService(out ITestSLcoreService3 requestedService3).Should().BeTrue();
        requestedService3.Should().BeSameAs(service3New).And.NotBeSameAs(service3);
    }

    private static void SetUpServiceCreation<T>(Mock<ISLCoreJsonRpc> rpcMock, T service) where T : class, ISLCoreService
    {
        rpcMock.Setup(x => x.CreateService<T>()).Returns(service);
    }

    private static void SetUpConnectionState(Mock<ISLCoreJsonRpc> rpcMock, bool isAlive)
    {
        rpcMock.SetupGet(x => x.IsAlive).Returns(isAlive);
    }

    private SLCoreServiceProvider CreateTestSubject(ISLCoreJsonRpc jsonRpc = null, IThreadHandling threadHandling = null, ILogger logger = null)
    {
        threadHandling ??= new NoOpThreadHandler();
        logger ??= new TestLogger();
        var testSubject = new SLCoreServiceProvider(threadHandling, logger);
        if (jsonRpc != null)
        {
            testSubject.SetCurrentConnection(jsonRpc);
        }
        return testSubject;
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
