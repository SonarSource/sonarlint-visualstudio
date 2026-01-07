/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Sentry;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry;

[TestClass]
public class MonitoringServiceTests
{
    private ISlCoreTelemetryHelper telemetryHelper;
    private IVsInfoProvider vsInfoProvider;
    private IThreadHandling threadHandling;
    private TestLogger logger;
    private ISentrySdk sentrySdk;

    private MonitoringService testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        telemetryHelper = Substitute.For<ISlCoreTelemetryHelper>();
        vsInfoProvider = Substitute.For<IVsInfoProvider>();
        threadHandling = Substitute.For<IThreadHandling>();
        sentrySdk = Substitute.For<ISentrySdk>();
        logger = new TestLogger();

        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>() )
            .Returns(callInfo => callInfo.Arg<Func<Task<int>>>()());

        sentrySdk.PushScope().Returns(new NoOpDisposable());
        sentrySdk.Init(Arg.Any<Action<SentryOptions>>()).Returns(new NoOpDisposable());

        testSubject = new MonitoringService(telemetryHelper, vsInfoProvider, threadHandling, logger, sentrySdk);
    }

    [TestMethod]
    public void Init_WhenTelemetryDisabled_QueriesTelemetryOnBackgroundThread_AndStaysInactive()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Disabled);

        testSubject.Init();

        telemetryHelper.Received(1).GetStatus();
        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());

        sentrySdk.DidNotReceive().Init(Arg.Any<Action<SentryOptions>>());

        testSubject.ReportException(new InvalidOperationException("boom"), "ctx");
        testSubject.Close();

        sentrySdk.DidNotReceive().CaptureException(Arg.Any<Exception>());
        sentrySdk.DidNotReceive().Close();
    }

    [TestMethod]
    public void Init_WhenTelemetryEnabled_InitializesSentry_AndReportExceptionCapturesException()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);

        testSubject.Init();

        sentrySdk.Received(1).Init(Arg.Any<Action<SentryOptions>>());

        var expected = new InvalidOperationException("boom");

        testSubject.ReportException(expected, "ctx");

        sentrySdk.Received(1).PushScope();
        sentrySdk.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
        sentrySdk.Received().CaptureException(expected);
    }

    [TestMethod]
    public void Close_WhenActive_ClosesSentry()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);
        testSubject.Init();

        testSubject.Close();

        sentrySdk.Received(1).Close();
    }

    [TestMethod]
    public void Init_WhenTelemetryEnabled_ConfiguresSentryOptions()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);

        var vsVersion = Substitute.For<IVsVersion>();
        vsVersion.DisplayVersion.Returns("17.0");
        vsInfoProvider.Version.Returns(vsVersion);

        Action<SentryOptions> capturedOptions = null;
        sentrySdk.Init(Arg.Do<Action<SentryOptions>>(x => capturedOptions = x)).Returns(new NoOpDisposable());

        testSubject.Init();

        Assert.IsNotNull(capturedOptions);

        var options = new SentryOptions();
        capturedOptions(options);

        Assert.IsNotNull(options.Dsn);
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Release));
        Assert.IsTrue(options.DefaultTags.ContainsKey("ideVersion"));
        Assert.AreEqual("17.0", options.DefaultTags["ideVersion"]);
        Assert.IsTrue(options.DefaultTags.ContainsKey("platform"));
        Assert.IsTrue(options.DefaultTags.ContainsKey("architecture"));
    }

    [TestMethod]
    public void Init_WhenSentryInitThrows_LogsAndStaysInactive()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);

        sentrySdk.Init(Arg.Any<Action<SentryOptions>>()).Returns(_ => throw new InvalidOperationException("boom"));

        testSubject.Init();

        logger.AssertPartialOutputStringExists("[MonitoringService] Failed to initialize Sentry");

        testSubject.ReportException(new InvalidOperationException("ignored"), "ctx");
        testSubject.Close();

        sentrySdk.DidNotReceive().CaptureException(Arg.Any<Exception>());
        sentrySdk.DidNotReceive().Close();
    }

    [TestMethod]
    public void Reinit_WhenInactive_InitializesSentry()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Disabled);
        testSubject.Init();

        sentrySdk.ClearReceivedCalls();

        testSubject.Reinit();

        sentrySdk.Received(1).Init(Arg.Any<Action<SentryOptions>>());
    }

    [TestMethod]
    public void Reinit_WhenActive_DoesNothing()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);
        testSubject.Init();

        sentrySdk.ClearReceivedCalls();

        testSubject.Reinit();

        sentrySdk.DidNotReceive().Init(Arg.Any<Action<SentryOptions>>());
        sentrySdk.DidNotReceive().Close();
        sentrySdk.DidNotReceive().CaptureException(Arg.Any<Exception>());
    }

    [TestMethod]
    public void ReportException_WhenCaptureThrows_LogsAndDoesNotRethrow()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);
        testSubject.Init();

        sentrySdk.When(x => x.CaptureException(Arg.Any<Exception>()))
            .Do(_ => throw new InvalidOperationException("Sentry failure"));

        testSubject.ReportException(new InvalidOperationException("original"), "ctx");

        logger.AssertPartialOutputStringExists("Failed to report exception to Sentry");
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
