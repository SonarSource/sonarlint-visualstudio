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

using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class SolutionStateTrackerTests
{
    private static readonly BindingConfiguration BindingConfiguration = new(new BoundServerProject("1", "2", new ServerConnection.SonarCloud("3")), SonarLintMode.Connected, "4");
    private static readonly BindingConfiguration BindingConfigurationUpdatedSettings = new(new BoundServerProject("1", "2", new ServerConnection.SonarCloud("3", new ServerConnectionSettings(false))), SonarLintMode.Connected, "4");
    private const string SolutionName = "solution";
    private SolutionStateTracker testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new SolutionStateTracker();

    [TestMethod]
    public void MefCtor_CheckIsExported_ISolutionStateTracker() => MefTestHelpers.CheckTypeCanBeImported<SolutionStateTracker, ISolutionStateTracker>();

    [TestMethod]
    public void MefCtor_CheckIsExported_ISolutionStateTrackerUpdater() => MefTestHelpers.CheckTypeCanBeImported<SolutionStateTracker, ISolutionStateTrackerUpdater>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SolutionStateTracker>();

    [TestMethod]
    public void Update_SetsNewStateAndRaisesEvent()
    {
        var eventHandler = Substitute.For<EventHandler<SolutionStateChangedEventArgs>>();
        testSubject.SolutionStateChanged += eventHandler;

        testSubject.Update(SolutionName, BindingConfiguration);

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SolutionStateChangedEventArgs>(x => x.SolutionState.SolutionName == SolutionName && x.SolutionState.BindingConfiguration == BindingConfiguration));
        testSubject.CurrentState.Should().BeEquivalentTo(new SolutionState(SolutionName, BindingConfiguration));
    }

    [TestMethod]
    public void Update_SettingsInsignificantlyUpdated_SetsNewStateButDoesNotRaiseEvent()
    {
        testSubject.Update(SolutionName, BindingConfiguration);
        var eventHandler = Substitute.For<EventHandler<SolutionStateChangedEventArgs>>();
        testSubject.SolutionStateChanged += eventHandler;

        testSubject.Update(SolutionName, BindingConfigurationUpdatedSettings);

        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
        testSubject.CurrentState.Should().BeEquivalentTo(new SolutionState(SolutionName, BindingConfigurationUpdatedSettings));
    }

    [TestMethod]
    public void Update_ChangeToDifferentSolution_SetsNewStateAndRaisesEvent()
    {
        const string solutionName2 = "solution2";
        testSubject.Update(SolutionName, BindingConfiguration);
        var eventHandler = Substitute.For<EventHandler<SolutionStateChangedEventArgs>>();
        testSubject.SolutionStateChanged += eventHandler;

        testSubject.Update(solutionName2, BindingConfiguration);

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SolutionStateChangedEventArgs>(x => x.SolutionState.SolutionName == solutionName2 && x.SolutionState.BindingConfiguration == BindingConfiguration));
        testSubject.CurrentState.Should().BeEquivalentTo(new SolutionState(solutionName2, BindingConfiguration));
    }
}
