/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Windows.Documents;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class StatusBarNotifierTests
    {
        [TestMethod]
        public void Ctor_VerifyIsRunningOnUiThread()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(op =>
                {
                    // Try to check that the product code is executed inside the "RunOnUIThread" call
                    serviceProvider.Invocations.Count.Should().Be(0);
                    op();
                    serviceProvider.Invocations.Count.Should().Be(1);
                });

            var testSubject = new StatusBarNotifier(serviceProvider.Object, threadHandling.Object);

            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Notify_DisplaysMessageAndSpinner(bool showSpinner)
        {
            var expectedMessage = Guid.NewGuid().ToString();
            var statusIcon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General as object;

            var statusBarMock = new Mock<IVsStatusbar>();
            var serviceProvider = SetupServiceProviderWithStatusBar(statusBarMock.Object);

            var testSubject = new StatusBarNotifier(serviceProvider, new NoOpThreadHandler());
            testSubject.Notify(expectedMessage, showSpinner);

            statusBarMock.Verify(x => x.SetText(expectedMessage), Times.Once);
            statusBarMock.Verify(x => x.Animation(showSpinner ? 1 : 0, ref statusIcon), Times.Once);
            statusBarMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Notify_VerifyIsRunningOnUiThread()
        {
            var statusBar = new Mock<IVsStatusbar>();
            var serviceProvider = SetupServiceProviderWithStatusBar(statusBar.Object);

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(op =>
                {
                    op();
                });

            var testSubject = new StatusBarNotifier(serviceProvider, threadHandling.Object);

            threadHandling.Reset();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(op =>
                {
                    statusBar.Verify(x => x.Animation(It.IsAny<int>(), ref It.Ref<object>.IsAny), Times.Never);
                    statusBar.Verify(x => x.SetText(It.IsAny<string>()), Times.Never);
                    op();
                    statusBar.Verify(x => x.Animation(It.IsAny<int>(), ref It.Ref<object>.IsAny), Times.Once);
                    statusBar.Verify(x => x.SetText(It.IsAny<string>()), Times.Once);
                });

            testSubject.Notify(It.IsAny<string>(), It.IsAny<bool>());

            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);
        }

        private IServiceProvider SetupServiceProviderWithStatusBar(IVsStatusbar statusBar = null)
        {
            statusBar ??= Mock.Of<IVsStatusbar>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(IVsStatusbar))).Returns(statusBar);

            return serviceProviderMock.Object;
        }
    }
}
