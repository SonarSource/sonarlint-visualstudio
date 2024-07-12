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
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry;

[TestClass]
public class SlCoreTelemetryHelperTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SlCoreTelemetryHelper, ISlCoreTelemetryHelper>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SlCoreTelemetryHelper>();
    }
    
    [TestMethod]
    public void GetStatus_RunsOnBackgroundThreadSynchronously()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.Run(Arg.Any<Func<Task<SlCoreTelemetryStatus>>>()).Returns(info => Task.Run(() => info.Arg<Func<Task<SlCoreTelemetryStatus>>>()()).GetAwaiter().GetResult());
        threadHandling.SwitchToBackgroundThread().Returns(new NoOpThreadHandler.NoOpAwaitable());
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        var testSubject = CreateTestSubject(serviceProvider, threadHandling);

        testSubject.GetStatus();

        Received.InOrder(() =>
        {
            threadHandling.Run(Arg.Any<Func<Task<SlCoreTelemetryStatus>>>());
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

        status.Should().Be(SlCoreTelemetryStatus.Unavailable);
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

        status.Should().Be(result ? SlCoreTelemetryStatus.Enabled : SlCoreTelemetryStatus.Disabled);
    }


    [TestMethod]
    public void GetStatus_NonCriticalException_LoggedAndSuppressed()
    {
        var testLogger = new TestLogger();
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        serviceProvider.TryGetTransientService(out Arg.Any<ISLCoreService>()).Throws(new NullReferenceException());
        var testSubject = CreateTestSubject(serviceProvider, logger: testLogger);

        var status = testSubject.GetStatus();

        status.Should().Be(SlCoreTelemetryStatus.Unavailable);
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
    public void Notify_RunsOnBackgroundThread()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()().GetAwaiter().GetResult());
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        serviceProvider.TryGetTransientService(out Arg.Any<ITelemetrySLCoreService>()).Returns(true);
        var testSubject = CreateTestSubject(serviceProvider, threadHandling);
        var telemetryProducer = Substitute.For<Action<ITelemetrySLCoreService>>();

        testSubject.Notify(telemetryProducer);
        
        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            serviceProvider.TryGetTransientService(out Arg.Any<ITelemetrySLCoreService>());
            telemetryProducer.Invoke(Arg.Any<ITelemetrySLCoreService>());
        });
    }
    
    [TestMethod]
    public void Notify_ServiceUnavailable_Discards()
    {
        var testSubject = CreateTestSubject();
        var telemetryProducer = Substitute.For<Action<ITelemetrySLCoreService>>();

        testSubject.Notify(telemetryProducer);
        
        telemetryProducer.DidNotReceiveWithAnyArgs().Invoke(default);
    }
    
    [TestMethod]
    public void Notify_ServiceAvailable_CallsTelemetryProducer()
    {
        var serviceProvider = CreateTelemetryService(out var telemetryService);
        var testSubject = CreateTestSubject(serviceProvider);
        var telemetryProducer = Substitute.For<Action<ITelemetrySLCoreService>>();

        testSubject.Notify(telemetryProducer);
        
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
    
    private SlCoreTelemetryHelper CreateTestSubject(ISLCoreServiceProvider slCoreServiceProvider = null, IThreadHandling threadHandling = null, ILogger logger = null)
    {
        slCoreServiceProvider ??= Substitute.For<ISLCoreServiceProvider>();
        threadHandling ??= new NoOpThreadHandler();
        logger ??= new TestLogger();
        return new SlCoreTelemetryHelper(slCoreServiceProvider, threadHandling, logger);
    }
}
