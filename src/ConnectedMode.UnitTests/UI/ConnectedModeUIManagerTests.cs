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

using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI
{
    [TestClass]
    public class ConnectedModeUIManagerTests
    {
        private ConnectedModeUIManager testSubject;
        private IConnectedModeServices connectedModeServices;
        private IConnectedModeBindingServices connectedModeBindingServices;

        [TestInitialize]
        public void TestInitialize()
        {
            connectedModeServices = Substitute.For<IConnectedModeServices>();
            connectedModeBindingServices = Substitute.For<IConnectedModeBindingServices>();
            connectedModeServices.ThreadHandling.Returns(Substitute.For<IThreadHandling>());
            testSubject = new ConnectedModeUIManager(connectedModeServices, connectedModeBindingServices);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ConnectedModeUIManager, IConnectedModeUIManager>(
                MefTestHelpers.CreateExport<IConnectedModeServices>(),
                MefTestHelpers.CreateExport<IConnectedModeBindingServices>());
        }

        [TestMethod]
        public void MefCtor_CheckIsNonShared() => MefTestHelpers.CheckIsNonSharedMefComponent<ConnectedModeUIManager>();

        [TestMethod]
        public void ShowManageBindingDialog_RunsOnUIThread()
        {
            testSubject.ShowManageBindingDialog();

            connectedModeServices.ThreadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
        }

        [TestMethod]
        public void ShowTrustConnectionDialog_RunsOnUIThread()
        {
            testSubject.ShowTrustConnectionDialog(new ServerConnection.SonarCloud("myOrg"), null);

            connectedModeServices.ThreadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
        }
    }
}
