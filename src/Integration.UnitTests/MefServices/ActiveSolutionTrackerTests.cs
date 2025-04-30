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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NSubstitute.Extensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class ActiveSolutionTrackerTests
{
    private ConfigurableServiceProvider serviceProvider;
    private SolutionMock solutionMock;
    private ISolutionInfoProvider solutionInfoProvider;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private MockableInitializationProcessor createdInitializationProcessor;
    private TestLogger testLogger;
    private NoOpThreadHandler threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        serviceProvider = new ConfigurableServiceProvider();
        solutionMock = new SolutionMock();
        serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        solutionInfoProvider.GetSolutionName().Returns((string)null);
        testLogger = new TestLogger();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ActiveSolutionTracker, IActiveSolutionTracker>(
            MefTestHelpers.CreateExport<SVsServiceProvider>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<ActiveSolutionTracker>();

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("A Solution")]
    public void ActiveSolutionTracker_InitializesCorrectly(string name)
    {
        solutionInfoProvider.GetSolutionName().Returns(name);
        var vsSolution = Substitute.For<IVsSolution>();
        serviceProvider = new ConfigurableServiceProvider();
        serviceProvider.RegisterService(typeof(SVsSolution), vsSolution);

        var testSubject = CreateAndInitializeTestSubject();

        testSubject.CurrentSolutionName.Should().Be(name);
        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<ActiveSolutionTracker>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0), Arg.Any<Func<IThreadHandling, Task>>());
            // this one is invoked by the ctor
            createdInitializationProcessor.InitializeAsync();
            threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
            solutionInfoProvider.GetSolutionName();
            vsSolution.AdviseSolutionEvents(testSubject, out _);
            // this one is invoked by CreateAndInitializeTestSubject
            createdInitializationProcessor.InitializeAsync();
        });
    }

    [TestMethod]
    public void ActiveSolutionTracker_DelaysServiceCallsUntilInitialization()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        var eventHandler = Substitute.For<EventHandler<ActiveSolutionChangedEventArgs>>();
        testSubject.ActiveSolutionChanged += eventHandler;

        solutionMock.SimulateSolutionOpen();
        solutionMock.SimulateSolutionClose();
        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
        solutionInfoProvider.ReceivedCalls().Should().BeEmpty();

        barrier.SetResult(1);
        solutionInfoProvider.Received(1).GetSolutionName();

        solutionMock.SimulateSolutionOpen();
        eventHandler.ReceivedWithAnyArgs(1).Invoke(default, default);
        solutionInfoProvider.Received(2).GetSolutionName();
    }

    [TestMethod]
    public void ActiveSolutionTracker_DisposeBeforeInitialized_DisposeAndInitializeDoNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        solutionInfoProvider.ClearReceivedCalls();
        var eventHandler = Substitute.For<EventHandler<ActiveSolutionChangedEventArgs>>();
        testSubject.ActiveSolutionChanged += eventHandler;
        testSubject.Dispose();
        barrier.SetResult(1);

        solutionMock.SimulateSolutionClose();
        solutionMock.SimulateSolutionOpen();

        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
        solutionInfoProvider.DidNotReceiveWithAnyArgs().GetSolutionName();
        solutionMock.AssertNoHierarchyEventSinks();
    }

    [TestMethod]
    public void ActiveSolutionTracker_Dispose()
    {
        var testSubject = CreateAndInitializeTestSubject();
        solutionInfoProvider.ClearReceivedCalls();
        var eventHandler = Substitute.For<EventHandler<ActiveSolutionChangedEventArgs>>();
        testSubject.ActiveSolutionChanged += eventHandler;
        testSubject.Dispose();

        solutionMock.SimulateSolutionClose();
        solutionMock.SimulateSolutionOpen();

        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
        solutionInfoProvider.DidNotReceiveWithAnyArgs().GetSolutionName();
    }

    [TestMethod]
    public void ActiveSolutionTracker_RaiseEventOnSolutionOpen()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var eventHandler = Substitute.For<EventHandler<ActiveSolutionChangedEventArgs>>();
        testSubject.ActiveSolutionChanged += eventHandler;
        solutionInfoProvider.GetSolutionName().Returns("name123");

        solutionMock.SimulateSolutionOpen();

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<ActiveSolutionChangedEventArgs>(x => x.SolutionName == "name123"));
    }

    [TestMethod]
    public void ActiveSolutionTracker_RaiseEventOnSolutionClose()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var eventHandler = Substitute.For<EventHandler<ActiveSolutionChangedEventArgs>>();
        testSubject.ActiveSolutionChanged += eventHandler;

        solutionMock.SimulateSolutionClose();

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<ActiveSolutionChangedEventArgs>(x => x.SolutionName == null));
    }

    [TestMethod]
    public void ActiveSolutionTracker_RaiseEventOnFolderOpen()
    {
        var testSubject = CreateAndInitializeTestSubject();
        var eventHandler = Substitute.For<EventHandler<ActiveSolutionChangedEventArgs>>();
        testSubject.ActiveSolutionChanged += eventHandler;
        solutionInfoProvider.GetSolutionName().Returns("name123");

        solutionMock.SimulateFolderOpen();

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<ActiveSolutionChangedEventArgs>(x => x.SolutionName == "name123"));
    }

    private ActiveSolutionTracker CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ActiveSolutionTracker>(threadHandling, new TestLogger(), processor =>
        {
            createdInitializationProcessor = processor;
            MockableInitializationProcessor.ConfigureWithWait(processor, tcs);
        });
        return new ActiveSolutionTracker(serviceProvider, solutionInfoProvider, initializationProcessorFactory);
    }

    private ActiveSolutionTracker CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ActiveSolutionTracker>(threadHandling, new TestLogger(), processor => createdInitializationProcessor = processor);
        var testSubject = new ActiveSolutionTracker(serviceProvider, solutionInfoProvider, initializationProcessorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }
}
