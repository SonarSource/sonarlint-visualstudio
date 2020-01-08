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
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Native;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class NativeInteropTests
    {
        private Mock<IWin32Window> win32WindowMock; 
        private Mock<INativeMethods> nativeMethodsMock;

        private NativeInterop testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            win32WindowMock = new Mock<IWin32Window>(MockBehavior.Strict);
            nativeMethodsMock = new Mock<INativeMethods>(MockBehavior.Strict);
            
            testSubject = new NativeInterop(nativeMethodsMock.Object);
        }

        [TestMethod]
        public void CloseWindow_NullWindow_WindowNotClosed()
        {
            // Act
            testSubject.CloseRootWindow(null);

            // Assert
            CheckGetAncestorNotCalled();
            CheckSendMessageNotCalled();
        }

        [TestMethod]
        public void CloseWindow_MissingWindowHandle_WindowNotClosed()
        {
            // Arrange
            SetWindowHandles(childHandle: IntPtr.Zero, ancestorHandle: null);

            // Act
            testSubject.CloseRootWindow(win32WindowMock.Object);

            // Assert
            CheckGetAncestorNotCalled();
            CheckSendMessageNotCalled();
        }

        [TestMethod]
        public void CloseWindow_NoAncestor_WindowNotClosed()
        {
            // Arrange
            SetWindowHandles(childHandle: new IntPtr(111), ancestorHandle: IntPtr.Zero);

            // Act
            testSubject.CloseRootWindow(win32WindowMock.Object);

            // Assert
            CheckGetAncestorIsCalled();
            CheckSendMessageNotCalled();
        }

        [TestMethod]
        public void CloseWindow_AncestorExists_WindowIsClosed()
        {
            // Arrange
            SetWindowHandles(childHandle: new IntPtr(111), ancestorHandle: new IntPtr(222));

            // Act
            testSubject.CloseRootWindow(win32WindowMock.Object);

            // Assert
            CheckGetAncestorIsCalled();
            CheckSendMessageIsCalled();
        }

        private void SetWindowHandles(IntPtr childHandle, IntPtr? ancestorHandle)
        {
            win32WindowMock.Setup(x => x.Handle).Returns(childHandle);

            if (ancestorHandle.HasValue)
            {
                nativeMethodsMock.Setup(x => x.GetAncestor(childHandle, NativeMethods.GetRootWindow)).Returns(ancestorHandle.Value);

                // The tests may not called SendMessage, but if it is called we expect the ancestor window handle to be passed
                nativeMethodsMock.Setup(x => x.SendMessage(ancestorHandle.Value, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero)).Returns(VSConstants.S_OK);
            }
        }

        private void CheckGetAncestorNotCalled() =>
            nativeMethodsMock.Verify(x => x.GetAncestor(It.IsAny<IntPtr>(), It.IsAny<uint>()), Times.Never);

        private void CheckGetAncestorIsCalled() =>
            nativeMethodsMock.Verify(x => x.GetAncestor(It.IsAny<IntPtr>(), It.IsAny<uint>()), Times.Once);

        private void CheckSendMessageNotCalled() =>
            nativeMethodsMock.Verify(x => x.SendMessage(It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<IntPtr>(), It.IsAny<IntPtr>()), Times.Never);

        private void CheckSendMessageIsCalled() =>
            nativeMethodsMock.Verify(x => x.SendMessage(It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<IntPtr>(), It.IsAny<IntPtr>()), Times.Once);
    }
}
