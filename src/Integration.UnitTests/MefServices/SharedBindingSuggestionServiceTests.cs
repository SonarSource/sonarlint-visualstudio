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

using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class SharedBindingSuggestionServiceTests
{
    private SharedBindingSuggestionService testSubject;
    private ISuggestSharedBindingGoldBar suggestSharedBindingGoldBar;
    private IConnectedModeServices connectedModeServices;
    private IConnectedModeBindingServices connectedModeBindingServices;
    private IActiveSolutionTracker activeSolutionTracker;

    [TestInitialize]
    public void TestInitialize()
    {
        suggestSharedBindingGoldBar = Substitute.For<ISuggestSharedBindingGoldBar>();
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();
        activeSolutionTracker = Substitute.For<IActiveSolutionTracker>();

        testSubject = new SharedBindingSuggestionService(suggestSharedBindingGoldBar, connectedModeServices, connectedModeBindingServices, activeSolutionTracker);
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<SharedBindingSuggestionService, ISharedBindingSuggestionService>(
            MefTestHelpers.CreateExport<ISuggestSharedBindingGoldBar>(),
            MefTestHelpers.CreateExport<IConnectedModeServices>(),
            MefTestHelpers.CreateExport<IConnectedModeBindingServices>(),
            MefTestHelpers.CreateExport<IActiveSolutionTracker>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SharedBindingSuggestionService>();
    }

    [TestMethod]
    public void Suggest_SharedBindingExistsAndIsStandalone_ShowsGoldBar()
    {
        MockSharedBindingConfigExists();
        MockSolutionMode(SonarLintMode.Standalone);

        testSubject.Suggest();
        
        suggestSharedBindingGoldBar.Received(1).Show(ServerType.SonarQube, Arg.Any<Action>());
    }

    [TestMethod]
    public void Suggest_SharedBindingExistsAndIsConnected_DoesNotShowGoldBar()
    {
        MockSharedBindingConfigExists();
        MockSolutionMode(SonarLintMode.Connected);

        testSubject.Suggest();

        suggestSharedBindingGoldBar.DidNotReceive().Show(ServerType.SonarQube, Arg.Any<Action>());
    }

    [TestMethod]
    public void Suggest_SharedBindingDoesNotExistAndIsStandAlone_DoesNotShowGoldBar()
    {
        MockSolutionMode(SonarLintMode.Standalone);

        testSubject.Suggest();

        suggestSharedBindingGoldBar.DidNotReceive().Show(ServerType.SonarQube, Arg.Any<Action>());
    }

    [TestMethod]
    public void ActiveSolutionChanged_SolutionIsOpened_ShowsGoldBar()
    {
        MockSharedBindingConfigExists();
        MockSolutionMode(SonarLintMode.Standalone);
        
        RaiseActiveSolutionChanged(true);

        suggestSharedBindingGoldBar.Received(1).Show(ServerType.SonarQube, Arg.Any<Action>());
    }

    [TestMethod]
    public void ActiveSolutionChanged_SolutionIsOpened_DoesNotShowGoldBar()
    {
        MockSharedBindingConfigExists();
        MockSolutionMode(SonarLintMode.Standalone);
        
        RaiseActiveSolutionChanged(false);

        suggestSharedBindingGoldBar.DidNotReceive().Show(ServerType.SonarQube, Arg.Any<Action>());
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromActiveSolutionChanged()
    {
        testSubject.Dispose();

        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
    }

    private void RaiseActiveSolutionChanged(bool isSolutionOpened)
    {
        activeSolutionTracker.ActiveSolutionChanged += Raise.EventWith(new ActiveSolutionChangedEventArgs(isSolutionOpened));
    }

    private void MockSolutionMode(SonarLintMode mode)
    {
        connectedModeServices.ConfigurationProvider.GetConfiguration().Returns(new BindingConfiguration(null, mode, string.Empty));
    }

    private void MockSharedBindingConfigExists()
    {
        connectedModeBindingServices.SharedBindingConfigProvider.GetSharedBinding().Returns(new SharedBindingConfigModel());
    }



}
