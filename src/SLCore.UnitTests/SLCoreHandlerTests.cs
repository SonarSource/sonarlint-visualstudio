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
        instanceHandler.StartInstanceAsync().Returns(neverCompletedTaskSource.Task);
        
        testSubject.EnableSloop();
        
        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            instanceHandler.StartInstanceAsync();
        });
        notificationService.DidNotReceiveWithAnyArgs().Show(default);
    }
    
    [TestMethod]
    public void EnableSloop_DisposedMidLifecycle_ExitsAndDoesNothing()
    {
        var lifeCycleTaskSource = new TaskCompletionSource<bool>();
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out _);
        instanceHandler.StartInstanceAsync().Returns(lifeCycleTaskSource.Task);
        instanceHandler.When(x => x.Dispose()).Do(_ => lifeCycleTaskSource.SetResult(true));
        
        testSubject.EnableSloop();
        testSubject.Dispose();
        
        instanceHandler.Received(1).StartInstanceAsync();
        notificationService.DidNotReceiveWithAnyArgs().Show(default);
    }
    
    [TestMethod]
    public void EnableSloop_Disposed_Throws()
    {
        var testSubject = CreateTestSubject(out var instanceHandler, out var notificationService, out var threadHandling);
        testSubject.Dispose();
        
        var act = () => testSubject.EnableSloop();

        act.Should().ThrowExactly<ObjectDisposedException>();
        instanceHandler.DidNotReceiveWithAnyArgs().StartInstanceAsync();
        threadHandling.DidNotReceiveWithAnyArgs().RunOnBackgroundThread(default(Func<Task<int>>));
        notificationService.DidNotReceiveWithAnyArgs().Show(default);
    }
    
    [DataTestMethod]
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
                instanceHandler.StartInstanceAsync();
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

    [DataTestMethod]
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
            var resetActon = (Action)notificationService.ReceivedCalls().Last().GetArguments().First();
            resetActon();
        }
        
        Received.InOrder(() =>
        {
            for (int i = 0; i < manualRestartsCount + 1; i++)
            {
                threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
                for (int j = 0; j < maxStartsBeforeManual; j++)
                {
                    instanceHandler.StartInstanceAsync();
                }
                notificationService.Show(Arg.Is<Action>(a => a != null));
            }
        });
    }
    
    private static void SetUpInstanceHandler(ISLCoreInstanceHandler instanceHandler)
    {
        var currentRun = 0;
        instanceHandler.CurrentStartNumber.Returns(_ => currentRun);
        instanceHandler.StartInstanceAsync().Returns(_ =>
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
        threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()());
        
        return maxStartsBeforeManual.HasValue
            ? new SLCoreHandler(instanceHandler, notificationService, maxStartsBeforeManual.Value, threadHandling)
            : new SLCoreHandler(instanceHandler, notificationService, threadHandling);
    }
}
