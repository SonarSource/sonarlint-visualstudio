/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class SharedBindingSuggestionServiceTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<SharedBindingSuggestionService, ISharedBindingSuggestionService>(
            MefTestHelpers.CreateExport<ISuggestSharedBindingGoldBar>(),
            MefTestHelpers.CreateExport<ITeamExplorerController>(),
            MefTestHelpers.CreateExport<IConnectedModeWindowEventBasedScheduler>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SharedBindingSuggestionService>();
    }

    [TestMethod]
    public void ConnectAfterTeamExplorerInitialized_ConnectedModeWindowLoaded_NotLoaded_ConnectScheduledForLater()
    {
        var testSubject = CreateTestSubject(out var bindingGoldBar, out var teamExplorerController, out var scheduler);
        testSubject.Suggest(ServerType.SonarQube, () => null);

        var callSequence = new MockSequence();

        scheduler.InSequence(callSequence).Setup(x => x.ScheduleActionOnNextEvent(It.IsAny<Action>()));
        teamExplorerController.InSequence(callSequence).Setup(x => x.ShowSonarQubePage());
        
        CallConnectHandler(bindingGoldBar);
        
        scheduler.Verify(x => x.ScheduleActionOnNextEvent(It.IsAny<Action>()), Times.Once);
        scheduler.VerifyNoOtherCalls();
        teamExplorerController.Verify(x => x.ShowSonarQubePage(), Times.Once);
        teamExplorerController.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void ConnectAfterTeamExplorerInitialized_ConnectedModeWindowLoaded_CallsConnectCommand()
    {
        var testSubject = CreateTestSubject(out var bindingGoldBar, out var teamExplorerController, out var scheduler);
        testSubject.Suggest(ServerType.SonarQube, CreateConnectCommandProvider(out var connectCommand));

        var callSequence = new MockSequence();

        teamExplorerController.InSequence(callSequence).Setup(x => x.ShowSonarQubePage());
        connectCommand.InSequence(callSequence)
            .Setup(x => x.CanExecute(testSubject.autobindEnabledConfiguration))
            .Returns(true);
        connectCommand.InSequence(callSequence)
            .Setup(x => x.Execute(testSubject.autobindEnabledConfiguration));
        
        CallConnectHandler(bindingGoldBar);
        
        scheduler.VerifyNoOtherCalls();
        teamExplorerController.Verify(x => x.ShowSonarQubePage(), Times.Once);
        teamExplorerController.VerifyNoOtherCalls();
        connectCommand.Verify(x => x.CanExecute(testSubject.autobindEnabledConfiguration), Times.Once);
        connectCommand.Verify(x => x.Execute(testSubject.autobindEnabledConfiguration), Times.Once);
        connectCommand.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void Suggest_HasServerType_ShowsGoldBar()
    {
        var testSubject = CreateTestSubject(out var bindingGoldBar, out _, out _);
        
        testSubject.Suggest(ServerType.SonarQube, CreateConnectCommandProvider(out _));
        
        bindingGoldBar.Verify(x => x.Show(ServerType.SonarQube, It.IsAny<Action>()), Times.Once);
    }

    [TestMethod]
    public void Suggest_NoServerType_DoesNotCallGoldBar()
    {
        var testSubject = CreateTestSubject(out var bindingGoldBar, out _, out _);
        
        testSubject.Suggest(null, CreateConnectCommandProvider(out _));
        
        bindingGoldBar.Verify(x => x.Show(It.IsAny<ServerType>(), It.IsAny<Action>()), Times.Never);
    }

    private void CallConnectHandler(Mock<ISuggestSharedBindingGoldBar> mock)
    {
        ((Action)mock.Invocations.Single().Arguments[1])();
    }
    
    private Func<ICommand<ConnectConfiguration>> CreateConnectCommandProvider(out Mock<ICommand<ConnectConfiguration>> connectCommandMock)
    {
        var commandMock = new Mock<ICommand<ConnectConfiguration>>();
        connectCommandMock = commandMock;
        return () => commandMock.Object;
    }
    
    private SharedBindingSuggestionService CreateTestSubject(out Mock<ISuggestSharedBindingGoldBar> bindingGoldBar, 
        out Mock<ITeamExplorerController> teamExplorerController,
        out Mock<IConnectedModeWindowEventBasedScheduler> connectedModeWindowEventBasedScheduler)
    {
        return new SharedBindingSuggestionService((bindingGoldBar = new Mock<ISuggestSharedBindingGoldBar>()).Object,
            (teamExplorerController = new Mock<ITeamExplorerController>()).Object,
            (connectedModeWindowEventBasedScheduler = new Mock<IConnectedModeWindowEventBasedScheduler>()).Object);
    }
}
