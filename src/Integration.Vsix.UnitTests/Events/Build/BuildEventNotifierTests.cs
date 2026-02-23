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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.Events.Build;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.UnitTests.Events.Build;

[TestClass]
public class BuildEventNotifierTests
{
    private IBuildEventUiManager buildEventUiManager = null!;
    private IInitializationProcessorFactory initializationProcessorFactory = null!;
    private IVsUIServiceOperation vsUiServiceOperation = null!;
    private TestLogger testLogger = null!;
    private NoOpThreadHandler threadHandling = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        buildEventUiManager = Substitute.For<IBuildEventUiManager>();
        vsUiServiceOperation = Substitute.For<IVsUIServiceOperation>();
        testLogger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<BuildEventNotifier, IBuildEventNotifier>(
            MefTestHelpers.CreateExport<IBuildEventUiManager>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<BuildEventNotifier>();

    [TestMethod]
    public void Ctor_SetsUpLogContext()
    {
        _ = CreateAndInitializeTestSubject();

        testLogger.Received().ForContext(Strings.BuildEventNotifier_LogContext);
    }

    [TestMethod]
    public void Initialize_SubscribesToBuildEvents()
    {
        var buildManager = Substitute.For<IVsSolutionBuildManager2>();
        SetupVsUiServiceOperation(buildManager);

        _ = CreateAndInitializeTestSubject();

        buildManager.Received().AdviseUpdateSolutionEvents(Arg.Any<IVsUpdateSolutionEvents>(), out Arg.Any<uint>());
    }

    [TestMethod]
    public void UpdateSolutionDone_CallsShowErrorNotificationDialog()
    {
        var testSubject = CreateAndInitializeTestSubject();

        InvokeUpdateSolutionDone(testSubject);

        buildEventUiManager.Received(1).ShowErrorNotificationDialog();
    }

    [TestMethod]
    public void UpdateSolutionDone_ExceptionThrown_LogsAndContinues()
    {
        buildEventUiManager.When(x => x.ShowErrorNotificationDialog()).Do(_ => throw new InvalidOperationException("Test exception"));
        var testSubject = CreateAndInitializeTestSubject();

        var result = InvokeUpdateSolutionDone(testSubject);

        result.Should().Be(VSConstants.S_OK);
        testLogger.AssertPartialOutputStringExists("Test exception");
    }

    [TestMethod]
    public void UpdateSolutionBegin_ReturnsSOK()
    {
        var testSubject = CreateAndInitializeTestSubject();

        var result = InvokeUpdateSolutionBegin(testSubject);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void UpdateSolutionDone_ReturnsSOK()
    {
        var testSubject = CreateAndInitializeTestSubject();

        var result = ((IVsUpdateSolutionEvents)testSubject).UpdateSolution_Done(0, 0, 0);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void UpdateSolutionStartUpdate_ReturnsSOK()
    {
        var testSubject = CreateAndInitializeTestSubject();
        int cancel = 0;

        var result = ((IVsUpdateSolutionEvents)testSubject).UpdateSolution_StartUpdate(ref cancel);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void UpdateSolutionCancel_ReturnsSOK()
    {
        var testSubject = CreateAndInitializeTestSubject();

        var result = ((IVsUpdateSolutionEvents)testSubject).UpdateSolution_Cancel();

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnActiveProjectCfgChange_ReturnsSOK()
    {
        var testSubject = CreateAndInitializeTestSubject();

        var result = ((IVsUpdateSolutionEvents)testSubject).OnActiveProjectCfgChange(null);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromBuildEvents()
    {
        var buildManager = Substitute.For<IVsSolutionBuildManager2>();
        SetupVsUiServiceOperation(buildManager);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        buildManager.Received(1).UnadviseUpdateSolutionEvents(Arg.Any<uint>());
    }

    [TestMethod]
    public void DisposeBeforeInitialized_DoesNotThrow()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);

        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
        barrier.SetResult(1);
    }

    private void SetupVsUiServiceOperation(IVsSolutionBuildManager2 buildManager)
    {
        vsUiServiceOperation
            .When(s => s.Execute<SVsSolutionBuildManager, IVsSolutionBuildManager2>(Arg.Any<Action<IVsSolutionBuildManager2>>()))
            .Do(callback =>
            {
                var action = callback.Arg<Action<IVsSolutionBuildManager2>>();
                action(buildManager);
            });
        vsUiServiceOperation
            .ExecuteAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>(Arg.Any<Action<IVsSolutionBuildManager2>>())
            .Returns(info =>
            {
                var action = info.Arg<Action<IVsSolutionBuildManager2>>();
                action(buildManager);
                return Task.CompletedTask;
            });
    }

    private static int InvokeUpdateSolutionBegin(BuildEventNotifier testSubject)
    {
        var cancel = 0;
        return ((IVsUpdateSolutionEvents)testSubject).UpdateSolution_Begin(ref cancel);
    }

    private static int InvokeUpdateSolutionDone(BuildEventNotifier testSubject) =>
        ((IVsUpdateSolutionEvents)testSubject).UpdateSolution_Done(0, 0, 0);

    private BuildEventNotifier CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<BuildEventNotifier>(
            threadHandling, testLogger, processor => MockableInitializationProcessor.ConfigureWithWait(processor, tcs));
        return new BuildEventNotifier(buildEventUiManager, initializationProcessorFactory, vsUiServiceOperation, testLogger);
    }

    private BuildEventNotifier CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<BuildEventNotifier>(threadHandling, testLogger);
        var testSubject = new BuildEventNotifier(buildEventUiManager, initializationProcessorFactory, vsUiServiceOperation, testLogger);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }
}
