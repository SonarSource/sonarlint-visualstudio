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
using System.Collections.Generic;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList;
using SonarLint.VisualStudio.IssueVisualization.Security.SelectionService;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsToolWindowTests
    {
        [TestMethod]
        public void Dispose_FrameIsClosed()
        {
            var frameMock = new Mock<IVsWindowFrame>();
            var testSubject = CreateTestSubject(frameMock: frameMock);

            frameMock.Verify(x => x.CloseFrame(It.IsAny<uint>()), Times.Never());

            testSubject.Dispose();

            frameMock.Verify(x => x.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave), Times.Once());
        }

        [TestMethod]
        public void Dispose_HotspotsControlIsDisposed()
        {
            var hotspotsTableControl = new Mock<IWpfTableControl>();
            var testSubject = CreateTestSubject(hotspotsTableControl);

            hotspotsTableControl.Verify(x=> x.Dispose(), Times.Never);

            testSubject.Dispose();

            hotspotsTableControl.Verify(x => x.Dispose(), Times.Once);
        }

        private HotspotsToolWindow CreateTestSubject(Mock<IWpfTableControl> hotspotsTableControl = null,
            Mock<IVsWindowFrame> frameMock = null)
        {
            hotspotsTableControl ??= new Mock<IWpfTableControl>();

            var wpfTableControlProviderMock = new Mock<IWpfTableControlProvider>();
            wpfTableControlProviderMock
                .Setup(x => x.CreateControl(
                    It.IsAny<ITableManager>(),
                    It.IsAny<bool>(),
                    It.IsAny<IEnumerable<ColumnState>>(),
                    It.IsAny<string[]>()))
                .Returns(hotspotsTableControl.Object);

            var componentModelMock = new Mock<IComponentModel>();

            componentModelMock
                .Setup(x => x.GetService<ITableManagerProvider>())
                .Returns(Mock.Of<ITableManagerProvider>());

            componentModelMock
                .Setup(x => x.GetService<IWpfTableControlProvider>())
                .Returns(wpfTableControlProviderMock.Object);

            componentModelMock
                .Setup(x => x.GetService<IHotspotsSelectionService>())
                .Returns(Mock.Of<IHotspotsSelectionService>());

            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(SComponentModel)))
                .Returns(componentModelMock.Object);

            frameMock ??= new Mock<IVsWindowFrame>();

            return new HotspotsToolWindow(serviceProviderMock.Object) {Frame = frameMock.Object};
        }
    }
}
