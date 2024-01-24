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

using System;
using FluentAssertions;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class TeamExplorerControllerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
             => MefTestHelpers.CheckTypeCanBeImported<TeamExplorerController, ITeamExplorerController>(
                    MefTestHelpers.CreateExport<IVsUIServiceOperation>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<TeamExplorerController>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            _ = CreateTestSubject(serviceOp.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceOp.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void ShowSonarQubePage_NavigatesToPage()
        {
            // Arrange
            var startPageId = new Guid(SonarQubePage.PageId);

            var teamExplorer = new Mock<ITeamExplorer>();
            var serviceOp = CreateServiceOperation(teamExplorer.Object);

            var testSubject = CreateTestSubject(serviceOp);

            // Act
            testSubject.ShowSonarQubePage();

            // Assert
            teamExplorer.Verify(x => x.NavigateToPage(startPageId, null), Times.Once);
        }

        private IVsUIServiceOperation CreateServiceOperation(ITeamExplorer svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<ITeamExplorer, ITeamExplorer>(It.IsAny<Action<ITeamExplorer>>()))
                .Callback<Action<ITeamExplorer>>(op => op(svcToPassToCallback));

            return serviceOp.Object;
        }

        private TeamExplorerController CreateTestSubject(IVsUIServiceOperation vSServiceOperation) =>
            new(vSServiceOperation);
    }
}
