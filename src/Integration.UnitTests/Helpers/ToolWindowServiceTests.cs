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
    public class ToolWindowServiceTests
    {
        private static readonly Guid ValidToolWindowId = Guid.NewGuid();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Set up a service provider with the required service
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(SVsUIShell))).Returns(Mock.Of<IVsUIShell>());

            var serviceProviderExport = MefTestHelpers.CreateExport<SVsServiceProvider>(serviceProviderMock.Object);

            // Act and Assert
            MefTestHelpers.CheckTypeCanBeImported<ToolWindowService, IToolWindowService>(null, new[] { serviceProviderExport });

            serviceProviderMock.VerifyAll();
        }

        [TestMethod]
        public void Show_FindWindowFailed_NoExceptionAndFrameNotShown()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            var uiShellMock = new Mock<IVsUIShell>();
            var frameMock = new Mock<IVsWindowFrame>();

            SetupFindToolWindow(serviceProviderMock, uiShellMock, VSConstants.E_OUTOFMEMORY, ValidToolWindowId, frameMock.Object);

            var testSubject = new ToolWindowService(serviceProviderMock.Object);

            // Act
            using (new AssertIgnoreScope())
            {
                testSubject.Show(ValidToolWindowId, true);
            }

            // Non-success HResult so shouldn't have attempted to show the frame
            frameMock.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Show_FindWindowSucceededButReturnedNullToolWindow_NoException()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            var uiShellMock = new Mock<IVsUIShell>();

            SetupFindToolWindow(serviceProviderMock, uiShellMock, VSConstants.S_OK, ValidToolWindowId, toolWindowObject: null);

            var testSubject = new ToolWindowService(serviceProviderMock.Object);

            Action act = () => testSubject.Show(ValidToolWindowId, true);
            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Show_FindWindowSucceededAndNonNullToolWindow_ToolWindowShown(bool shouldActivateWindow)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            var uiShellMock = new Mock<IVsUIShell>();
            var frameMock = new Mock<IVsWindowFrame>();

            SetupFindToolWindow(serviceProviderMock, uiShellMock, VSConstants.S_OK, ValidToolWindowId, frameMock.Object);

            var testSubject = new ToolWindowService(serviceProviderMock.Object);

            // Act
            testSubject.Show(ValidToolWindowId, shouldActivateWindow);

            var expectedShowCallCount = shouldActivateWindow ? 1 : 0;
            frameMock.Verify(x => x.Show(), Times.Exactly(expectedShowCallCount));
        }

        private void SetupFindToolWindow(Mock<IServiceProvider> serviceProvider, Mock<IVsUIShell> uiShell, int hrResult, Guid toolWindowId, IVsWindowFrame toolWindowObject)
        {
            serviceProvider.Setup(x => x.GetService(typeof(SVsUIShell))).Returns(uiShell.Object);

            uiShell
                .Setup(x => x.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowId, out toolWindowObject))
                .Returns(hrResult);
        }
    }
}
