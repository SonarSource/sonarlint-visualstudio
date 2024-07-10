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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry;

[TestClass]
public class TelemetryChangeHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<TelemetryChangeHandler, ITelemetryChangeHandler>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<TelemetryChangeHandler>();
    }
    
    [TestMethod]
    public void GetStatus_RunsOnBackgroundThreadSynchronously()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.Run(Arg.Any<Func<Task<bool?>>>()).Returns(info => Task.Run(() => info.Arg<Func<Task<bool?>>>()()).GetAwaiter().GetResult());
        threadHandling.SwitchToBackgroundThread().Returns(new NoOpThreadHandler.NoOpAwaitable());
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        var testSubject = CreateTestSubject(serviceProvider, threadHandling);

        testSubject.GetStatus();

        Received.InOrder(() =>
        {
            threadHandling.Run(Arg.Any<Func<Task<bool?>>>());
            threadHandling.SwitchToBackgroundThread();
            serviceProvider.TryGetTransientService(out Arg.Any<ITelemetrySLCoreService>());
        });
    }

    [TestMethod]
    public void GetStatus_ServiceNotAvailable_ReturnsNull()
    {
        var testLogger = new TestLogger();
        var testSubject = CreateTestSubject(logger: testLogger);

        var status = testSubject.GetStatus();

        status.Should().BeNull();
        testLogger.OutputStrings.Should().BeEmpty();
    }
    
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetStatus_ServiceAvailable_ReturnsServiceCallResult(bool result)
    {
        var serviceProvider = CreateTelemetryService(out var telemetryService);
        telemetryService.GetStatusAsync().Returns(new GetStatusResponse(result));
        var testSubject = CreateTestSubject(serviceProvider);

        var status = testSubject.GetStatus();

        status.Should().Be(result);
    }


    [TestMethod]
    public void GetStatus_NonCriticalException_LoggedAndSuppressed()
    {
        var testLogger = new TestLogger();
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        serviceProvider.TryGetTransientService(out Arg.Any<ISLCoreService>()).Throws(new NullReferenceException());
        var testSubject = CreateTestSubject(serviceProvider, logger: testLogger);

        var status = testSubject.GetStatus();

        status.Should().BeNull();
        testLogger.OutputStrings.Should().HaveCount(1);
    }
    
    [TestMethod]
    public void GetStatus_CriticalException_Throws()
    {
        var testLogger = new TestLogger();
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        serviceProvider.TryGetTransientService(out Arg.Any<ISLCoreService>()).Throws(new DivideByZeroException());
        var testSubject = CreateTestSubject(serviceProvider, logger: testLogger);

        var act = () => testSubject.GetStatus();

        act.Should().Throw<DivideByZeroException>();
    }

    [TestMethod]
    public void SendTelemetry_RunsOnBackgroundThread()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()().GetAwaiter().GetResult());
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        serviceProvider.TryGetTransientService(out Arg.Any<ITelemetrySLCoreService>()).Returns(true);
        var testSubject = CreateTestSubject(serviceProvider, threadHandling);
        var telemetryProducer = Substitute.For<Action<ITelemetrySLCoreService>>();

        testSubject.SendTelemetry(telemetryProducer);
        
        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            serviceProvider.TryGetTransientService(out Arg.Any<ITelemetrySLCoreService>());
            telemetryProducer.Invoke(Arg.Any<ITelemetrySLCoreService>());
        });
    }
    
    [TestMethod]
    public void SendTelemetry_ServiceUnavailable_Discards()
    {
        var testSubject = CreateTestSubject();
        var telemetryProducer = Substitute.For<Action<ITelemetrySLCoreService>>();

        testSubject.SendTelemetry(telemetryProducer);
        
        telemetryProducer.DidNotReceiveWithAnyArgs().Invoke(default);
    }
    
    [TestMethod]
    public void SendTelemetry_ServiceAvailable_CallsTelemetryProducer()
    {
        var serviceProvider = CreateTelemetryService(out var telemetryService);
        var testSubject = CreateTestSubject(serviceProvider);
        var telemetryProducer = Substitute.For<Action<ITelemetrySLCoreService>>();

        testSubject.SendTelemetry(telemetryProducer);
        
        telemetryProducer.Received().Invoke(telemetryService);
    }
    
    private static ISLCoreServiceProvider CreateTelemetryService(out ITelemetrySLCoreService telemetryService)
    {
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        var serviceMock = Substitute.For<ITelemetrySLCoreService>();
        telemetryService = serviceMock;
        serviceProvider.TryGetTransientService(out Arg.Any<ITelemetrySLCoreService>()).Returns(info =>
        {
            info[0] = serviceMock;
            return true;
        });
        return serviceProvider;
    }
    
    private TelemetryChangeHandler CreateTestSubject(ISLCoreServiceProvider slCoreServiceProvider = null, IThreadHandling threadHandling = null, ILogger logger = null)
    {
        slCoreServiceProvider ??= Substitute.For<ISLCoreServiceProvider>();
        threadHandling ??= new NoOpThreadHandler();
        logger ??= new TestLogger();
        return new TelemetryChangeHandler(slCoreServiceProvider, threadHandling, logger);
    }
}
