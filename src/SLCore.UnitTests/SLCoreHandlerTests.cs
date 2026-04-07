/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.Linq;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Notification;

namespace SonarLint.VisualStudio.SLCore.UnitTests;

[TestClass]
public class SLCoreHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreHandler, ISLCoreHandler>(
            MefTestHelpers.CreateExport<ISLCoreInstanceHandler>(),
            MefTestHelpers.CreateExport<ISloopRestartFailedNotificationService>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreHandler>();
    }

    [TestMethod]
    public void EnableSloop_LaunchesSloopInTheBackgroundAndWaits()
    {
        var neverCompletedTaskSource = new TaskCompletionSource<bool>();
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out var threadHandling);
        instanceHandler.StartInstanceAsync(Arg.Any<CancellationToken>()).Returns(neverCompletedTaskSource.Task);

        testSubject.EnableSloop();

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            instanceHandler.StartInstanceAsync(Arg.Any<CancellationToken>());
        });
        notificationService.DidNotReceiveWithAnyArgs().Show(default);
    }

    [TestMethod]
    public void EnableSloop_DisposedMidLifecycle_ExitsAndDoesNothing()
    {
        var lifeCycleTaskSource = new TaskCompletionSource<bool>();
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out _);
        instanceHandler.StartInstanceAsync(Arg.Any<CancellationToken>()).Returns(lifeCycleTaskSource.Task);
        instanceHandler.When(x => x.Dispose()).Do(_ => lifeCycleTaskSource.SetResult(true));

        testSubject.EnableSloop();
        testSubject.Dispose();

        instanceHandler.Received(1).StartInstanceAsync(Arg.Any<CancellationToken>());
        notificationService.DidNotReceiveWithAnyArgs().Show(default);
    }

    [TestMethod]
    public void EnableSloop_Disposed_Throws()
    {
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out var threadHandling);
        testSubject.Dispose();

        var act = () => testSubject.EnableSloop();

        act.Should().ThrowExactly<ObjectDisposedException>();
        instanceHandler.DidNotReceiveWithAnyArgs().StartInstanceAsync(default);
        threadHandling.DidNotReceiveWithAnyArgs().RunOnBackgroundThread(default(Func<Task<int>>));
        notificationService.DidNotReceiveWithAnyArgs().Show(default);
    }

    [TestMethod]
    public void EnableSloop_CalledTwice_SecondCallIsNoOp()
    {
        var neverCompletedTaskSource = new TaskCompletionSource<bool>();
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out var threadHandling);
        instanceHandler.StartInstanceAsync(Arg.Any<CancellationToken>()).Returns(neverCompletedTaskSource.Task);

        testSubject.EnableSloop();
        testSubject.EnableSloop();

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        notificationService.DidNotReceiveWithAnyArgs().Show(default);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(10)]
    public void EnableSloop_AutoRestartsUpToLimit(int maxStartsBeforeManual)
    {
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out var threadHandling, maxStartsBeforeManual);
        SetUpInstanceHandler(instanceHandler);

        testSubject.EnableSloop();

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            for (int i = 0; i < maxStartsBeforeManual; i++)
            {
                instanceHandler.StartInstanceAsync(Arg.Any<CancellationToken>());
            }
            notificationService.Show(Arg.Is<Action>(a => a != null));
        });
    }

    [TestMethod]
    public void Dispose_DisposesInstanceHandler()
    {
        var testSubject = CreateTestSubject(out var instanceHandler, out _, out _);

        testSubject.Dispose();

        instanceHandler.Received().Dispose();
    }

    [TestMethod]
    public void ForceRestartSloop_Disposed_Throws()
    {
        var testSubject = CreateTestSubject(out _, out _, out _);
        testSubject.Dispose();

        var act = () => testSubject.ForceRestartSloop();

        act.Should().ThrowExactly<ObjectDisposedException>();
    }

    [TestMethod]
    public void ForceRestartSloop_CancelsActiveRun_AndRestartsOnBackgroundThread()
    {
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out var threadHandling, 1);
        SetUpInstanceHandler(instanceHandler);

        testSubject.EnableSloop();
        instanceHandler.ClearReceivedCalls();
        notificationService.ClearReceivedCalls();

        testSubject.ForceRestartSloop();

        instanceHandler.Received(1).StartInstanceAsync(Arg.Any<CancellationToken>());
        notificationService.Received(1).Show(Arg.Is<Action>(a => a != null));
    }

    [TestMethod]
    public void ForceRestartSloop_ResetsInitiatedStartAtCount()
    {
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out _, 3);
        SetUpInstanceHandler(instanceHandler);

        testSubject.EnableSloop();
        var totalStartsAfterFirstRun = instanceHandler.CurrentStartNumber;
        totalStartsAfterFirstRun.Should().Be(3);

        testSubject.ForceRestartSloop();

        instanceHandler.Received(6).StartInstanceAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(10)]
    public void EnableSloop_MultipleUserInitiatedRestarts_KeepsAutoRestartingUpToTheLimit(int maxStartsBeforeManual)
    {
        const int manualRestartsCount = 5;
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out var threadHandling, maxStartsBeforeManual);
        SetUpInstanceHandler(instanceHandler);

        testSubject.EnableSloop();
        for (int i = 0; i < manualRestartsCount; i++)
        {
            var resetAction = (Action)notificationService.ReceivedCalls().Last().GetArguments().First();
            resetAction();
        }

        instanceHandler.Received(maxStartsBeforeManual * (manualRestartsCount + 1)).StartInstanceAsync(Arg.Any<CancellationToken>());
        notificationService.Received(manualRestartsCount + 1).Show(Arg.Is<Action>(a => a != null));
    }

    private static void SetUpInstanceHandler(ISLCoreInstanceHandler instanceHandler)
    {
        var currentRun = 0;
        instanceHandler.CurrentStartNumber.Returns(_ => currentRun);
        instanceHandler.StartInstanceAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            currentRun++;
            return Task.CompletedTask;
        });
    }

    private SLCoreHandler CreateTestSubject(
        out ISLCoreInstanceHandler instanceHandler,
        out ISloopRestartFailedNotificationService notificationService,
        out IThreadHandling threadHandling,
        int? maxStartsBeforeManual = null)
    {
        instanceHandler = Substitute.For<ISLCoreInstanceHandler>();
        notificationService = Substitute.For<ISloopRestartFailedNotificationService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        return maxStartsBeforeManual.HasValue
            ? new SLCoreHandler(instanceHandler, notificationService, maxStartsBeforeManual.Value, threadHandling)
            : new SLCoreHandler(instanceHandler, notificationService, threadHandling);
    }
}
