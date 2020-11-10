/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class OutputWindowServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<OutputWindowService, IOutputWindowService>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
                MefTestHelpers.CreateExport<IToolWindowService>(Mock.Of<IToolWindowService>())
            });
        }

        [TestMethod]
        public void Show_FailedToGetSonarLintPane_OutputWindowNotShown()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            SetupSonarLintPane(serviceProvider, sonarLintPane: null);
            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = new OutputWindowService(serviceProvider.Object, toolWindowServiceMock.Object);

            using (new AssertIgnoreScope())
            {
                testSubject.Show();
            }

            serviceProvider.VerifyAll();
            serviceProvider.VerifyNoOtherCalls();
            toolWindowServiceMock.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Show_FailedToActivateSonarLintPane_OutputWindowNotShown()
        {
            var sonarLintPane = new Mock<IVsOutputWindowPane>();
            sonarLintPane.Setup(x => x.Activate()).Returns(VSConstants.E_FAIL);

            var serviceProvider = new Mock<IServiceProvider>();
            SetupSonarLintPane(serviceProvider, sonarLintPane.Object);

            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = new OutputWindowService(serviceProvider.Object, toolWindowServiceMock.Object);

            using (new AssertIgnoreScope())
            {
                testSubject.Show();
            }

            serviceProvider.VerifyAll();
            serviceProvider.VerifyNoOtherCalls();

            toolWindowServiceMock.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Show_ActivatedSonarLintPane_GotOutputWindow_OutputWindowShown()
        {
            var sonarLintPane = new Mock<IVsOutputWindowPane>();
            sonarLintPane.Setup(x => x.Activate()).Returns(VSConstants.S_OK);

            var serviceProvider = new Mock<IServiceProvider>();
            SetupSonarLintPane(serviceProvider, sonarLintPane.Object);

            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = new OutputWindowService(serviceProvider.Object, toolWindowServiceMock.Object);

            // Act
            testSubject.Show();

            toolWindowServiceMock.Verify(x => x.Show(VSConstants.StandardToolWindows.Output), Times.Once);
        }

        private static void SetupSonarLintPane(Mock<IServiceProvider> serviceProvider, IVsOutputWindowPane sonarLintPane)
        {
            var outputWindow = new Mock<IVsOutputWindow>();
            outputWindow
                .Setup(x => x.GetPane(ref VsShellUtils.SonarLintOutputPaneGuid, out sonarLintPane))
                .Returns(VSConstants.S_OK);

            serviceProvider
                .Setup(x => x.GetService(typeof(SVsOutputWindow)))
                .Returns(outputWindow.Object);
        }
    }
}
