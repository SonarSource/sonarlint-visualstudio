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

using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.Events;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Events;

[TestClass]
public class BuildEventNotifierTests
{
    private ILocalIssuesStore localIssuesStore;
    private IMessageBox messageBox;
    private IToolWindowService toolWindowService;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private IVsUIServiceOperation vsUIServiceOperation;
    private TestLogger testLogger;
    private NoOpThreadHandler threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        localIssuesStore = Substitute.For<ILocalIssuesStore>();
        messageBox = Substitute.For<IMessageBox>();
        toolWindowService = Substitute.For<IToolWindowService>();
        vsUIServiceOperation = Substitute.For<IVsUIServiceOperation>();
        testLogger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<BuildEventNotifier, IBuildEventNotifier>(
            MefTestHelpers.CreateExport<ILocalIssuesStore>(),
            MefTestHelpers.CreateExport<IMessageBox>(),
            MefTestHelpers.CreateExport<IToolWindowService>(),
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
        SetupVsUIServiceOperation(buildManager);

        _ = CreateAndInitializeTestSubject();

        buildManager.Received().AdviseUpdateSolutionEvents(Arg.Any<IVsUpdateSolutionEvents>(), out Arg.Any<uint>());
    }

    [TestMethod]
    public void UpdateSolutionDone_NoIssues_DoesNotShowMessageBox()
    {
        localIssuesStore.GetAll().Returns([]);
        var testSubject = CreateAndInitializeTestSubject();

        InvokeUpdateSolutionDone(testSubject);

        messageBox.DidNotReceiveWithAnyArgs().Show(default, default, default, default);
    }

    [TestMethod]
    public void UpdateSolutionDone_HasErrorIssues_ShowsMessageBox()
    {
        var issues = new[] { CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR) };
        localIssuesStore.GetAll().Returns(issues);
        var testSubject = CreateAndInitializeTestSubject();

        InvokeUpdateSolutionDone(testSubject);

        messageBox.Received(1).Show(
            string.Format(Strings.BuildEventNotifier_IssuesFoundMessage, 1),
            Strings.BuildEventNotifier_Caption,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    [TestMethod]
    public void UpdateSolutionDone_HasOnlyNonErrorIssues_DoesNotShowMessageBox()
    {
        var issues = new[]
        {
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_WARNING),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_MESSAGE)
        };
        localIssuesStore.GetAll().Returns(issues);
        var testSubject = CreateAndInitializeTestSubject();

        InvokeUpdateSolutionDone(testSubject);

        messageBox.DidNotReceiveWithAnyArgs().Show(default, default, default, default);
    }

    [TestMethod]
    public void UpdateSolutionDone_HasMultipleErrorIssues_ShowsCorrectCount()
    {
        var issues = new[]
        {
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR)
        };
        localIssuesStore.GetAll().Returns(issues);
        var testSubject = CreateAndInitializeTestSubject();

        InvokeUpdateSolutionDone(testSubject);

        messageBox.Received(1).Show(
            string.Format(Strings.BuildEventNotifier_IssuesFoundMessage, 3),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(),
            Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public void UpdateSolutionDone_HasMixedSeverityIssues_CountsOnlyErrors()
    {
        var issues = new[]
        {
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_WARNING),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_MESSAGE)
        };
        localIssuesStore.GetAll().Returns(issues);
        var testSubject = CreateAndInitializeTestSubject();

        InvokeUpdateSolutionDone(testSubject);

        messageBox.Received(1).Show(
            string.Format(Strings.BuildEventNotifier_IssuesFoundMessage, 2),
            Arg.Any<string>(),
            Arg.Any<MessageBoxButton>(),
            Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public void UpdateSolutionDone_UserClicksOk_OpensErrorList()
    {
        var issues = new[] { CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR) };
        localIssuesStore.GetAll().Returns(issues);
        messageBox.Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>())
            .Returns(MessageBoxResult.OK);
        var testSubject = CreateAndInitializeTestSubject();

        InvokeUpdateSolutionDone(testSubject);

        toolWindowService.Received(1).Show(IssueListIds.ErrorListId);
    }

    [TestMethod]
    public void UpdateSolutionDone_ExceptionDuringIssueCheck_LogsAndContinues()
    {
        localIssuesStore.GetAll().Throws(new InvalidOperationException("Test exception"));
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
        SetupVsUIServiceOperation(buildManager);
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();

        buildManager.Received().UnadviseUpdateSolutionEvents(Arg.Any<uint>());
    }

    [TestMethod]
    public void DisposeBeforeInitialized_DoesNotThrow()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);

        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
        barrier.SetResult(1);
    }

    private void SetupVsUIServiceOperation(IVsSolutionBuildManager2 buildManager)
    {
        vsUIServiceOperation
            .When(s => s.Execute<SVsSolutionBuildManager, IVsSolutionBuildManager2>(Arg.Any<Action<IVsSolutionBuildManager2>>()))
            .Do(callback =>
            {
                var action = callback.Arg<Action<IVsSolutionBuildManager2>>();
                action(buildManager);
            });
    }

    private static int InvokeUpdateSolutionBegin(BuildEventNotifier testSubject)
    {
        int cancel = 0;
        return ((IVsUpdateSolutionEvents)testSubject).UpdateSolution_Begin(ref cancel);
    }

    private static int InvokeUpdateSolutionDone(BuildEventNotifier testSubject)
    {
        return ((IVsUpdateSolutionEvents)testSubject).UpdateSolution_Done(0, 0, 0);
    }

    private BuildEventNotifier CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<BuildEventNotifier>(
            threadHandling, testLogger, processor => MockableInitializationProcessor.ConfigureWithWait(processor, tcs));
        return new BuildEventNotifier(localIssuesStore, messageBox, toolWindowService, initializationProcessorFactory, vsUIServiceOperation, testLogger);
    }

    private BuildEventNotifier CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<BuildEventNotifier>(threadHandling, testLogger);
        var testSubject = new BuildEventNotifier(localIssuesStore, messageBox, toolWindowService, initializationProcessorFactory, vsUIServiceOperation, testLogger);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private static IAnalysisIssueVisualization CreateIssueWithSeverity(__VSERRORCATEGORY severity)
    {
        var issue = Substitute.For<IAnalysisIssueVisualization>();
        issue.VsSeverity.Returns(severity);
        return issue;
    }
}
