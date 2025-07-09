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

using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class InitializationTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Sloop_StartedAndStoppedWithoutErrors()
    {
        var infrastructureLogger = new TestLogger();
        var slCoreStdErrorLogger = new TestLogger();
        var rpcLogger = new TestLogger();
        using (var slCoreTestRunner = new SLCoreTestRunner(infrastructureLogger, slCoreStdErrorLogger, TestContext.TestName))
        {
            slCoreTestRunner.AddListener(new LoggerListener(rpcLogger));
            await slCoreTestRunner.Start(rpcLogger);
        }

        VerifyNoErrorsInLogs(infrastructureLogger);
        VerifyNoErrorsInLogs(slCoreStdErrorLogger);
        VerifyLogMessagesReceived(rpcLogger);
        rpcLogger.AssertPartialOutputStringExists("Telemetry disabled on server startup");
        rpcLogger.AssertPartialOutputStringDoesNotExist("Internal error");
    }

    [TestMethod]
    public async Task Sloop_ConfigScopeSetAndUnsetWithoutErrors()
    {
        const string configScopeId = "ConfigScope1";
        var infrastructureLogger = new TestLogger(logVerbose: false);
        var rpcLogger = new TestLogger();
        var slCoreStdErrorLogger = new TestLogger();
        var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
        var analysisListener = SetUpAnalysisListenerToUnblockOnAnalysisReady(configScopeId, analysisReadyCompletionSource);

        using (var slCoreTestRunner = new SLCoreTestRunner(infrastructureLogger, slCoreStdErrorLogger, TestContext.TestName))
        {
            slCoreTestRunner.AddListener(new LoggerListener(rpcLogger));
            slCoreTestRunner.AddListener(new ProgressListener(Substitute.For<IStatusBarNotifier>()));
            slCoreTestRunner.AddListener(analysisListener);
            await slCoreTestRunner.Start(rpcLogger);

            var activeConfigScopeTracker = new ActiveConfigScopeTracker(slCoreTestRunner.SLCoreServiceProvider,
                new AsyncLockFactory(),
                new NoOpThreadHandler(),
                infrastructureLogger);

            activeConfigScopeTracker.SetCurrentConfigScope(configScopeId);

            await WaitForAnalysisReadiness(analysisReadyCompletionSource);

            activeConfigScopeTracker.RemoveCurrentConfigScope();
        }

        VerifyAnalysisReadinessReached(analysisListener, configScopeId);
        VerifyNoErrorsInLogs(infrastructureLogger);
        VerifyLogMessagesReceived(rpcLogger);
    }

    private static void VerifyAnalysisReadinessReached(IAnalysisListener analysisListener, string configScopeId)
    {
        analysisListener.Received(1).DidChangeAnalysisReadiness(
            Arg.Is<DidChangeAnalysisReadinessParams>(d =>
                d.areReadyForAnalysis && d.configurationScopeIds.Contains(configScopeId)));
    }

    private static async Task WaitForAnalysisReadiness(TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource) =>
        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisReadyCompletionSource.Task, "analysis readiness");

    private static IAnalysisListener SetUpAnalysisListenerToUnblockOnAnalysisReady(
        string configScopeId,
        TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource)
    {
        var analysisListener = Substitute.For<IAnalysisListener>();
        analysisListener.When(l =>
                l.DidChangeAnalysisReadiness(Arg.Is<DidChangeAnalysisReadinessParams>(a =>
                    a.areReadyForAnalysis && a.configurationScopeIds.Contains(configScopeId))))
            .Do(info => analysisReadyCompletionSource.SetResult(info.Arg<DidChangeAnalysisReadinessParams>()));
        return analysisListener;
    }

    private static void VerifyLogMessagesReceived(TestLogger slCoreLogger)
    {
        slCoreLogger.OutputStrings.Should().HaveCountGreaterThan(0);
    }

    private static void VerifyNoErrorsInLogs(TestLogger testLogger)
    {
        testLogger.AssertNoOutputMessages();
    }
}
