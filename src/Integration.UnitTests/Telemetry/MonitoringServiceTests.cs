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
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

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

    [TestMethod]
    public void ReportException_WhenLocalRpcExceptionWithCommonErrorData_SetsExtras()
    {
        InitWithTelemetryEnabled();
        Scope capturedScope = null;
        sentrySdk.When(x => x.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(ci =>
            {
                capturedScope = new Scope(new SentryOptions());
                ci.Arg<Action<Scope>>()(capturedScope);
            });

        var innerException = new InvalidOperationException("inner error");
        var commonErrorData = new CommonErrorData(innerException);
        var localRpcException = new LocalRpcException("rpc failed") { ErrorCode = (int)JsonRpcErrorCode.InternalError, ErrorData = commonErrorData };

        testSubject.ReportException(localRpcException, "rpc_context");

        capturedScope.Should().NotBeNull();
        capturedScope.Extra.ContainsKey("rpc.errorData.typeName").Should().BeTrue();
        capturedScope.Extra["rpc.errorData.typeName"].Should().Be(commonErrorData.TypeName);
        capturedScope.Extra.ContainsKey("rpc.errorData.message").Should().BeTrue();
        capturedScope.Extra["rpc.errorData.message"].Should().Be(commonErrorData.Message);
        capturedScope.Extra.ContainsKey("rpc.errorData.stackTrace").Should().BeTrue();
        capturedScope.Extra["rpc.errorData.stackTrace"].Should().Be(commonErrorData.StackTrace);
        capturedScope.Extra.ContainsKey("rpc.errorData.hResult").Should().BeTrue();
        capturedScope.Extra["rpc.errorData.hResult"].Should().Be(commonErrorData.HResult);
    }

    [TestMethod]
    public void ReportException_WhenNotLocalRpcException_DoesNotSetErrorDataExtras()
    {
        InitWithTelemetryEnabled();
        Scope capturedScope = null;
        sentrySdk.When(x => x.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(ci =>
            {
                capturedScope = new Scope(new SentryOptions());
                ci.Arg<Action<Scope>>()(capturedScope);
            });

        var regularException = new InvalidOperationException("regular error");

        testSubject.ReportException(regularException, "ctx");

        capturedScope.Should().NotBeNull();
        capturedScope.Extra.ContainsKey("rpc.errorData.typeName").Should().BeFalse();
        capturedScope.Extra.ContainsKey("rpc.errorData.message").Should().BeFalse();
        capturedScope.Extra.ContainsKey("rpc.errorData.stackTrace").Should().BeFalse();
        capturedScope.Extra.ContainsKey("rpc.errorData.hResult").Should().BeFalse();
    }

    [TestMethod]
    public void ReportException_WhenLocalRpcExceptionWithoutCommonErrorData_DoesNotSetErrorDataExtras()
    {
        InitWithTelemetryEnabled();
        Scope capturedScope = null;
        sentrySdk.When(x => x.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(ci =>
            {
                capturedScope = new Scope(new SentryOptions());
                ci.Arg<Action<Scope>>()(capturedScope);
            });

        var localRpcException = new LocalRpcException("rpc failed") { ErrorCode = (int)JsonRpcErrorCode.InternalError, ErrorData = "some string data" };

        testSubject.ReportException(localRpcException, "rpc_context");

        capturedScope.Should().NotBeNull();
        capturedScope.Extra.ContainsKey("rpc.errorData.typeName").Should().BeFalse();
        capturedScope.Extra.ContainsKey("rpc.errorData.message").Should().BeFalse();
        capturedScope.Extra.ContainsKey("rpc.errorData.stackTrace").Should().BeFalse();
        capturedScope.Extra.ContainsKey("rpc.errorData.hResult").Should().BeFalse();
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
