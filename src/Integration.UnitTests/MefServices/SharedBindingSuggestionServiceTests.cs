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
using SonarLint.VisualStudio.ConnectedMode.UI;
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

    [TestInitialize]
    public void TestInitialize()
    {
        suggestSharedBindingGoldBar = Substitute.For<ISuggestSharedBindingGoldBar>();
        connectedModeServices = Substitute.For<IConnectedModeServices>();
        connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();

        testSubject = new SharedBindingSuggestionService(suggestSharedBindingGoldBar, connectedModeServices, connectedModeBindingServices);
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<SharedBindingSuggestionService, ISharedBindingSuggestionService>(
            MefTestHelpers.CreateExport<ISuggestSharedBindingGoldBar>(),
            MefTestHelpers.CreateExport<IConnectedModeServices>(),
            MefTestHelpers.CreateExport<IConnectedModeBindingServices>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SharedBindingSuggestionService>();
    }

    [TestMethod]
    public void Suggest_HasServerType_ShowsGoldBar()
    {
        testSubject.Suggest(ServerType.SonarQube);
        
        suggestSharedBindingGoldBar.Received(1).Show(ServerType.SonarQube, Arg.Any<Action>());
    }

    [TestMethod]
    public void Suggest_NoServerType_DoesNotCallGoldBar()
    {
        testSubject.Suggest(null);
        
        suggestSharedBindingGoldBar.DidNotReceive().Show(Arg.Any<ServerType>(), Arg.Any<Action>());
    }

    [TestMethod]
    public void Close_ClosesGoldBar()
    {
        testSubject.Close();

        suggestSharedBindingGoldBar.Received(1).Close();
    }
}
