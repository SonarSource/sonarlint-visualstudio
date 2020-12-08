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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Security.UI.TaintList;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.TaintList
{
    [TestClass]
    public class TaintToolWindowTests
    {
        [TestMethod]
        public void Dispose_FrameIsClosed()
        {
            var frameMock = new Mock<IVsWindowFrame>();
            var testSubject = CreateTestSubject(frameMock);

            frameMock.Verify(x => x.CloseFrame(It.IsAny<uint>()), Times.Never());

            testSubject.Dispose();

            frameMock.Verify(x => x.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave), Times.Once());
        }

        private TaintToolWindow CreateTestSubject(Mock<IVsWindowFrame> frameMock)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();

            frameMock ??= new Mock<IVsWindowFrame>();

            return new TaintToolWindow(serviceProviderMock.Object) {Frame = frameMock.Object};
        }
    }
}
