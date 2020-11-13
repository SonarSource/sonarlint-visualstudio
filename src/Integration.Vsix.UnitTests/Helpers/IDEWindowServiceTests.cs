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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Native;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class IDEWindowServiceTests
    {
        private readonly IntPtr ValidHandle = new IntPtr(123);
        private readonly WINDOWPLACEMENT Minimized = new WINDOWPLACEMENT { showCmd = NativeMethods.SW_SHOWMINIMIZED };
        private readonly WINDOWPLACEMENT NotMinimized = new WINDOWPLACEMENT { showCmd = 0 };

        private Mock<INativeMethods> nativeMock;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            nativeMock = new Mock<INativeMethods>();
            logger = new TestLogger(logToConsole: true);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IDEWindowService, IIDEWindowService>(null,
                new[] { MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>()) });
        }

        [TestMethod]
        public void BringToFront_InvalidHandle_NoApiCalls()
        {
            var testSubject = new IDEWindowService(nativeMock.Object, IntPtr.Zero, logger);

            // Act
            testSubject.BringToFront();

            nativeMock.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void BringToFront_GetWindowPlacementFails_IsStillBroughtToFront()
        {
            var testSubject = new IDEWindowService(nativeMock.Object, ValidHandle, logger);

            SetGetPlacementResponse(false, new WINDOWPLACEMENT());

            // Act
            testSubject.BringToFront();

            CheckGetPlacementIsCalled();
            CheckShowWindowIsNotCalled();
            CheckSetForegroundIsCalled();
        }

        [TestMethod]
        public void BringToFront_NotMinimized_IsBroughtToFrontButNotRestored()
        {
            var testSubject = new IDEWindowService(nativeMock.Object, ValidHandle, logger);

            SetGetPlacementResponse(result: true, NotMinimized);

            // Act
            testSubject.BringToFront();

            CheckGetPlacementIsCalled();
            CheckShowWindowIsNotCalled();
            CheckSetForegroundIsCalled();
        }

        [TestMethod]
        public void BringToFront_IsMinimized_IsRestoredAndBroughtToFront()
        {
            var testSubject = new IDEWindowService(nativeMock.Object, ValidHandle, logger);

            SetGetPlacementResponse(result: true, Minimized);

            // Act
            testSubject.BringToFront();

            CheckGetPlacementIsCalled();
            CheckShowWindowIsCalled();
            CheckSetForegroundIsCalled();
        }

        [TestMethod]
        public void BringToFront_NonCriticalExceptions_IsSuppressed()
        {
            var testSubject = new IDEWindowService(nativeMock.Object, ValidHandle, logger);
            nativeMock.Setup(x => x.SetForegroundWindow(ValidHandle))
                .Throws(new InvalidOperationException("thrown from test code"));

            // Act
            testSubject.BringToFront();

            logger.AssertPartialOutputStringExists("thrown from test code");
        }

        private void CheckGetPlacementIsCalled() =>
            nativeMock.Verify(x => x.GetWindowPlacement(ValidHandle, ref It.Ref<WINDOWPLACEMENT>.IsAny), Times.Once);

        // Delegate required to enable setting ref parameter in the mock
        private delegate bool GetWindowPlacementDelegate(IntPtr hwnd, ref WINDOWPLACEMENT placement);

        private void SetGetPlacementResponse(bool result, WINDOWPLACEMENT placementToReturn)
        {
            nativeMock.Setup(x => x.GetWindowPlacement(It.IsAny<IntPtr>(), ref It.Ref<WINDOWPLACEMENT>.IsAny))
                .Returns(new GetWindowPlacementDelegate((IntPtr _, ref WINDOWPLACEMENT placement) =>
                    {
                        placement = placementToReturn;
                        return result;
                    }));
        }

        private void CheckShowWindowIsCalled() =>
            nativeMock.Verify(x => x.ShowWindow(It.IsAny<IntPtr>(), NativeMethods.SW_RESTORE), Times.Once);

        private void CheckShowWindowIsNotCalled() =>
            nativeMock.Verify(x => x.ShowWindow(It.IsAny<IntPtr>(), It.IsAny<int>()), Times.Never);

        private void CheckSetForegroundIsCalled() =>
            nativeMock.Verify(x => x.SetForegroundWindow(It.IsAny<IntPtr>()), Times.Once);
    }
}
