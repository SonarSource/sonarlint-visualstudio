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
using Sentry.Protocol;
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
    private IDogfoodingService dogfoodingService;

    private MonitoringService testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        telemetryHelper = Substitute.For<ISlCoreTelemetryHelper>();
        vsInfoProvider = Substitute.For<IVsInfoProvider>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        sentrySdk = Substitute.For<ISentrySdk>();
        dogfoodingService = Substitute.For<IDogfoodingService>();
        logger = new TestLogger();
        sentrySdk.PushScope().Returns(new NoOpDisposable());
        sentrySdk.Init(Arg.Any<Action<SentryOptions>>()).Returns(new NoOpDisposable());
        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void Init_WhenTelemetryDisabled_QueriesTelemetryOnBackgroundThread_AndStaysInactive()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Disabled);

        testSubject.Init();
        testSubject.ReportException(new InvalidOperationException("boom"), "ctx");
        testSubject.Close();

        telemetryHelper.Received().GetStatus();
        threadHandling.Received().RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        sentrySdk.DidNotReceive().Init(Arg.Any<Action<SentryOptions>>());
        sentrySdk.DidNotReceive().CaptureException(Arg.Any<Exception>());
        sentrySdk.DidNotReceive().Close();
    }

    [TestMethod]
    public void Init_WhenTelemetryEnabled_InitializesSentry_AndReportExceptionCapturesException()
    {
        var expected = new InvalidOperationException("boom");
        InitWithTelemetryEnabled();

        testSubject.ReportException(expected, "ctx");

        sentrySdk.Received(1).Init(Arg.Any<Action<SentryOptions>>());
        sentrySdk.Received(1).PushScope();
        sentrySdk.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
        sentrySdk.Received().CaptureException(expected);
    }

    [TestMethod]
    public void ReportException_SetsContextAndExplicitCaptureTag()
    {
        InitWithTelemetryEnabled();
        Action<Scope> configureScope = null;
        sentrySdk.ConfigureScope(Arg.Do<Action<Scope>>(x => configureScope = x));

        testSubject.ReportException(new InvalidOperationException("boom"), "ctx");

        configureScope.Should().NotBeNull();
        var scope = new Scope(new SentryOptions());
        configureScope(scope);
        scope.Tags.ContainsKey("slvs_context").Should().BeTrue();
        scope.Tags["slvs_context"].Should().Be("ctx");
        scope.Tags.ContainsKey(MonitoringService.ExplicitCaptureTag).Should().BeTrue();
        scope.Tags[MonitoringService.ExplicitCaptureTag].Should().Be("true");
    }

    [TestMethod]
    public void FilterSentryEvent_WhenExplicitCapture_ReturnsEvent()
    {
        var sentryEvent = new SentryEvent();
        sentryEvent.SetTag(MonitoringService.ExplicitCaptureTag, "true");

        var result = MonitoringService.FilterSentryEvent(sentryEvent);

        result.Should().BeSameAs(sentryEvent);
    }

    [TestMethod]
    public void FilterSentryEvent_WhenUnhandledWithSonarLintFrame_ReturnsEvent()
    {
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Mechanism = new Mechanism { Handled = false },
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = new[] { new SentryStackFrame { Module = "SonarLint.VisualStudio.SLCore.Listeners" } }
                    }
                }
            }
        };

        var result = MonitoringService.FilterSentryEvent(sentryEvent);

        result.Should().BeSameAs(sentryEvent);
    }

    [TestMethod]
    public void FilterSentryEvent_WhenUnhandledWithoutSonarFrames_ReturnsNull()
    {
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Mechanism = new Mechanism { Handled = false },
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = new[] { new SentryStackFrame { Module = "Other.Assembly" } }
                    }
                }
            }
        };

        var result = MonitoringService.FilterSentryEvent(sentryEvent);

        result.Should().BeNull();
    }

    [TestMethod]
    public void FilterSentryEvent_WhenHandled_ReturnsNull()
    {
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Mechanism = new Mechanism { Handled = true },
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = new[] { new SentryStackFrame { Module = "SonarLint.VisualStudio.SLCore.Listeners" } }
                    }
                }
            }
        };

        var result = MonitoringService.FilterSentryEvent(sentryEvent);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Close_WhenActive_ClosesSentry()
    {
        InitWithTelemetryEnabled();

        testSubject.Close();

        sentrySdk.Received(1).Close();
    }

    [TestMethod]
    public void Init_WhenTelemetryEnabled_ConfiguresSentryOptions()
    {
        var vsVersion = Substitute.For<IVsVersion>();
        vsVersion.DisplayVersion.Returns("17.0");
        vsInfoProvider.Version.Returns(vsVersion);
        Action<SentryOptions> capturedOptions = null;
        sentrySdk.Init(Arg.Do<Action<SentryOptions>>(x => capturedOptions = x)).Returns(new NoOpDisposable());

        InitWithTelemetryEnabled();

        capturedOptions.Should().NotBeNull();
        var options = new SentryOptions();
        capturedOptions(options);
        options.Dsn.Should().NotBeNullOrWhiteSpace();
        options.Release.Should().NotBeNullOrWhiteSpace();
        options.DefaultTags.Should().ContainKey("ideVersion");
        options.DefaultTags["ideVersion"].Should().Be("17.0");
        options.DefaultTags.Should().ContainKey("platform");
        options.DefaultTags.Should().ContainKey("architecture");
    }

    [TestMethod]
    public void Init_WhenSentryInitThrows_LogsAndStaysInactive()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);
        sentrySdk.Init(Arg.Any<Action<SentryOptions>>()).Returns(_ => throw new InvalidOperationException("boom"));

        testSubject.Init();
        testSubject.ReportException(new InvalidOperationException("ignored"), "ctx");
        testSubject.Close();

        logger.AssertPartialOutputStringExists("Failed to initialize Sentry");
        sentrySdk.DidNotReceive().CaptureException(Arg.Any<Exception>());
        sentrySdk.DidNotReceive().Close();
    }

    [TestMethod]
    public void Reinit_WhenInactive_InitializesSentry()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Disabled);
        testSubject.Init();
        sentrySdk.ClearReceivedCalls();
        sentrySdk.Init(Arg.Any<Action<SentryOptions>>()).Returns(new NoOpDisposable());

        testSubject.Reinit();

        sentrySdk.Received(1).Init(Arg.Any<Action<SentryOptions>>());
    }

    [TestMethod]
    public void Reinit_WhenActive_DoesNothing()
    {
        InitWithTelemetryEnabled();
        sentrySdk.ClearReceivedCalls();

        testSubject.Reinit();

        sentrySdk.DidNotReceive().Init(Arg.Any<Action<SentryOptions>>());
        sentrySdk.DidNotReceive().Close();
        sentrySdk.DidNotReceive().CaptureException(Arg.Any<Exception>());
    }

    [TestMethod]
    public void ReportException_WhenCaptureThrows_LogsAndDoesNotRethrow()
    {
        InitWithTelemetryEnabled();
        sentrySdk.When(x => x.CaptureException(Arg.Any<Exception>()))
            .Do(_ => throw new InvalidOperationException("Sentry failure"));

        testSubject.ReportException(new InvalidOperationException("original"), "ctx");

        logger.AssertPartialOutputStringExists("Failed to report exception to Sentry");
    }

    private MonitoringService CreateTestSubject() =>
        new(telemetryHelper, vsInfoProvider, threadHandling, logger, sentrySdk, dogfoodingService);

    private void InitWithTelemetryEnabled()
    {
        telemetryHelper.GetStatus().Returns(SlCoreTelemetryStatus.Enabled);
        testSubject.Init();
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
