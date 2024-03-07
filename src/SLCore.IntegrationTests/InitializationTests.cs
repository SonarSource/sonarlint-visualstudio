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

using NSubstitute;
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
        var testLogger = new TestLogger();
        var slCoreLogger = new TestLogger();
        using (var slCoreTestRunner = new SLCoreTestRunner(testLogger, TestContext.TestName))
        {
            slCoreTestRunner.AddListener(new LoggerListener(slCoreLogger));
            await slCoreTestRunner.Start();
        }

        VerifyNoErrorsInLogs(testLogger);
        VerifyLogMessagesReceived(slCoreLogger);
    }
    
    [TestMethod]
    public async Task Sloop_ConfigScopeSetAndUnsetWithoutErrors()
    {
        const string configScopeId = "ConfigScope1";
        var testLogger = new TestLogger();
        var slCoreLogger = new TestLogger();
        var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
        var analysisListener = SetUpAnalysisListenerToUnblockOnAnalysisReady(configScopeId, analysisReadyCompletionSource);

        using (var slCoreTestRunner = new SLCoreTestRunner(testLogger, TestContext.TestName))
        {
            slCoreTestRunner.AddListener(new LoggerListener(slCoreLogger));
            slCoreTestRunner.AddListener(new ProgressListener());
            slCoreTestRunner.AddListener(analysisListener);
            await slCoreTestRunner.Start();

            var activeConfigScopeTracker = new ActiveConfigScopeTracker(slCoreTestRunner.SlCoreServiceProvider,
                new AsyncLockFactory(),
                new NoOpThreadHandler());

            activeConfigScopeTracker.SetCurrentConfigScope(configScopeId);
            
            await WaitForAnalysisReadiness(analysisReadyCompletionSource);

            activeConfigScopeTracker.RemoveCurrentConfigScope();
        }

        VerifyAnalysisReadinessReached(analysisListener, configScopeId);
        VerifyNoErrorsInLogs(testLogger);
        VerifyLogMessagesReceived(slCoreLogger);
    }

    private static void VerifyAnalysisReadinessReached(IAnalysisListener analysisListener, string configScopeId)
    {
        analysisListener.Received(1).DidChangeAnalysisReadinessAsync(
            Arg.Is<DidChangeAnalysisReadinessParams>(d =>
                d.areReadyForAnalysis && d.configurationScopeIds.Contains(configScopeId)));
    }

    private static async Task WaitForAnalysisReadiness(TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource)
    {
        var whenAny = await Task.WhenAny(analysisReadyCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        if (whenAny != analysisReadyCompletionSource.Task)
        {
            Assert.Fail("timeout reached");
        }
    }

    private static IAnalysisListener SetUpAnalysisListenerToUnblockOnAnalysisReady(string configScopeId,
        TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource)
    {
        var analysisListener = Substitute.For<IAnalysisListener>();
        analysisListener
            .DidChangeAnalysisReadinessAsync(
                Arg.Is<DidChangeAnalysisReadinessParams>(a =>
                    a.areReadyForAnalysis && a.configurationScopeIds.Contains(configScopeId)))
            .Returns(info =>
            {
                analysisReadyCompletionSource.SetResult(info.Arg<DidChangeAnalysisReadinessParams>());
                return Task.CompletedTask;
            });
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
