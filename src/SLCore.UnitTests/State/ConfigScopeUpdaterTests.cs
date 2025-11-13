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

using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.UnitTests.State;

[TestClass]
public class ConfigScopeUpdaterTests
{
    private IActiveConfigScopeTracker activeConfigScopeTrackerMock;
    private ISolutionInfoProvider solutionInfoProviderMock;
    private IThreadHandling threadHandlingMock;
    private ISLCoreHandler slCoreHandlerMock;
    private ConfigScopeUpdater testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTrackerMock = Substitute.For<IActiveConfigScopeTracker>();
        solutionInfoProviderMock = Substitute.For<ISolutionInfoProvider>();
        threadHandlingMock = Substitute.ForPartsOf<NoOpThreadHandler>();
        slCoreHandlerMock = Substitute.For<ISLCoreHandler>();
        testSubject = CreateTestSubject(activeConfigScopeTrackerMock, solutionInfoProviderMock, threadHandlingMock, slCoreHandlerMock);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ConfigScopeUpdater, IConfigScopeUpdater>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ISLCoreHandler>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ConfigScopeUpdater>();
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_CallsTrackerOnBackgroundThread()
    {
        solutionInfoProviderMock.GetSolutionName().Returns("sln");

        testSubject.UpdateConfigScopeForCurrentSolution(null);

        threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        threadHandlingMock.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_UnboundSolutionOpen_SetsCurrentConfigScope()
    {
        solutionInfoProviderMock.GetSolutionName().Returns("sln");

        testSubject.UpdateConfigScopeForCurrentSolution(null);

        activeConfigScopeTrackerMock.Received(1).SetCurrentConfigScope("sln", null, null);
        activeConfigScopeTrackerMock.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_BoundSolutionOpen_SetsCurrentConfigScope()
    {
        var serverConnection = new ServerConnection.SonarQube(new Uri("http://localhost"));
        var binding = new BoundServerProject("solution", "projectKey", serverConnection);
        solutionInfoProviderMock.GetSolutionName().Returns("sln");

        testSubject.UpdateConfigScopeForCurrentSolution(binding);

        activeConfigScopeTrackerMock.Received(1).SetCurrentConfigScope("sln", serverConnection.Id, binding.ServerProjectKey);
        activeConfigScopeTrackerMock.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_BoundSolutionWithOrganizationOpen_SetsCurrentConfigScope()
    {
        var serverConnection = new ServerConnection.SonarCloud("org");
        var binding = new BoundServerProject("solution", "projectKey", serverConnection);
        solutionInfoProviderMock.GetSolutionName().Returns("sln");

        testSubject.UpdateConfigScopeForCurrentSolution(binding);

        activeConfigScopeTrackerMock.Received(1).SetCurrentConfigScope("sln", serverConnection.Id, binding.ServerProjectKey);
        activeConfigScopeTrackerMock.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_SolutionClosed_RemovesCurrentConfigScope()
    {
        solutionInfoProviderMock.GetSolutionName().ReturnsNull();

        testSubject.UpdateConfigScopeForCurrentSolution(null);

        activeConfigScopeTrackerMock.Received(1).RemoveCurrentConfigScope();
        activeConfigScopeTrackerMock.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_WhenServiceProviderNotInitialized_ShowsNotification()
    {
        activeConfigScopeTrackerMock
            .When(x => x.SetCurrentConfigScope(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException(SLCoreStrings.ServiceProviderNotInitialized));
        solutionInfoProviderMock.GetSolutionName().Returns("sln");

        testSubject.UpdateConfigScopeForCurrentSolution(null);

        slCoreHandlerMock.Received(1).ShowNotificationIfNeeded();
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_WhenOtherInvalidOperationException_DoesNotShowNotification()
    {
        activeConfigScopeTrackerMock
            .When(x => x.SetCurrentConfigScope(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("Some other message"));
        solutionInfoProviderMock.GetSolutionName().Returns("sln");

        Assert.ThrowsException<InvalidOperationException>(() => testSubject.UpdateConfigScopeForCurrentSolution(null));
        slCoreHandlerMock.DidNotReceive().ShowNotificationIfNeeded();
    }

    private static ConfigScopeUpdater CreateTestSubject(
        IActiveConfigScopeTracker activeConfigScopeTracker = null,
        ISolutionInfoProvider solutionInfoProvider = null,
        IThreadHandling threadHandling = null,
        ISLCoreHandler slCoreHandler = null)
    {
        activeConfigScopeTracker ??= Substitute.For<IActiveConfigScopeTracker>();
        solutionInfoProvider ??= Substitute.For<ISolutionInfoProvider>();
        threadHandling ??= new NoOpThreadHandler();
        slCoreHandler ??= Substitute.For<ISLCoreHandler>();
        return new ConfigScopeUpdater(activeConfigScopeTracker, solutionInfoProvider, new Lazy<ISLCoreHandler>(() => slCoreHandler), threadHandling);
    }
}
