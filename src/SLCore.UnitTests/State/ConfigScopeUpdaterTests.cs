﻿/*
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.UnitTests.State;

[TestClass]
public class ConfigScopeUpdaterTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ConfigScopeUpdater, IConfigScopeUpdater>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
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
        var mockSequence = new MockSequence();
        var threadHandlingMock = new Mock<IThreadHandling>();
        threadHandlingMock
            .InSequence(mockSequence)
            .Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<int>>>()))
            .Callback((Func<Task<int>> action) => action());
        var solutionInfoProviderMock = new Mock<ISolutionInfoProvider>();
        solutionInfoProviderMock.InSequence(mockSequence).Setup(x => x.GetSolutionName()).Returns("sln");
        var activeConfigScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        activeConfigScopeTrackerMock.Setup(x =>
            x.SetCurrentConfigScope(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        var testSubject = CreateTestSubject(activeConfigScopeTrackerMock.Object, solutionInfoProviderMock.Object, threadHandling: threadHandlingMock.Object);

        testSubject.UpdateConfigScopeForCurrentSolution(null);

        threadHandlingMock.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<int>>>()));
        threadHandlingMock.VerifyNoOtherCalls();
    }


    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_UnboundSolutionOpen_SetsCurrentConfigScope()
    {
        var activeConfigScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        var solutionInfoProviderMock = new Mock<ISolutionInfoProvider>();
        solutionInfoProviderMock.Setup(x => x.GetSolutionName()).Returns("sln");
        var testSubject = CreateTestSubject(activeConfigScopeTrackerMock.Object, solutionInfoProviderMock.Object);

        testSubject.UpdateConfigScopeForCurrentSolution(null);

        activeConfigScopeTrackerMock.Verify(x => x.SetCurrentConfigScope("sln", null, null));
        activeConfigScopeTrackerMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_BoundSolutionOpen_SetsCurrentConfigScope()
    {
        var serverConnection = new ServerConnection.SonarQube(new Uri("http://localhost"));
        var binding = new BoundServerProject("solution", "projectKey", serverConnection);
        var activeConfigScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        var solutionInfoProviderMock = new Mock<ISolutionInfoProvider>();
        solutionInfoProviderMock.Setup(x => x.GetSolutionName()).Returns("sln");
        var testSubject = CreateTestSubject(activeConfigScopeTrackerMock.Object, solutionInfoProviderMock.Object);

        testSubject.UpdateConfigScopeForCurrentSolution(binding);

        activeConfigScopeTrackerMock.Verify(x => x.SetCurrentConfigScope("sln", serverConnection.Id, binding.ServerProjectKey));
        activeConfigScopeTrackerMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_BoundSolutionWithOrganizationOpen_SetsCurrentConfigScope()
    {
        var serverConnection = new ServerConnection.SonarCloud("org");
        var binding = new BoundServerProject("solution", "projectKey", serverConnection);
        var activeConfigScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        var solutionInfoProviderMock = new Mock<ISolutionInfoProvider>();
        solutionInfoProviderMock.Setup(x => x.GetSolutionName()).Returns("sln");
        var testSubject = CreateTestSubject(activeConfigScopeTrackerMock.Object, solutionInfoProviderMock.Object);

        testSubject.UpdateConfigScopeForCurrentSolution(binding);

        activeConfigScopeTrackerMock.Verify(x => x.SetCurrentConfigScope("sln", serverConnection.Id, binding.ServerProjectKey));
        activeConfigScopeTrackerMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void UpdateConfigScopeForCurrentSolution_SolutionClosed_RemovesCurrentConfigScope()
    {
        var activeConfigScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        var testSubject = CreateTestSubject(activeConfigScopeTrackerMock.Object);

        testSubject.UpdateConfigScopeForCurrentSolution(null);

        activeConfigScopeTrackerMock.Verify(x => x.RemoveCurrentConfigScope());
        activeConfigScopeTrackerMock.VerifyNoOtherCalls();
    }

    private static ConfigScopeUpdater CreateTestSubject(IActiveConfigScopeTracker activeConfigScopeTracker = null,
        ISolutionInfoProvider solutionInfoProvider = null,
        IThreadHandling threadHandling = null)
    {
        activeConfigScopeTracker ??= Mock.Of<IActiveConfigScopeTracker>();
        solutionInfoProvider ??= Mock.Of<ISolutionInfoProvider>();
        threadHandling ??= new NoOpThreadHandler();
        return new ConfigScopeUpdater(activeConfigScopeTracker, solutionInfoProvider, threadHandling);
    }
}
