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

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class ToolWindowServiceTests
    {
        private static readonly Guid ValidToolWindowId = Guid.NewGuid();

        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<ToolWindowService, IToolWindowService>(
                MefTestHelpers.CreateExport<IVsUIServiceOperation>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<ToolWindowService>();

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
        public void Show_FindWindowFailed_NoExceptionAndFrameNotShown()
        {
            var uiShellMock = new Mock<IVsUIShell>();
            var frameMock = new Mock<IVsWindowFrame>();
            SetupFindToolWindow(uiShellMock, VSConstants.E_OUTOFMEMORY, ValidToolWindowId, frameMock.Object);

            var serviceOp = CreateServiceOperation(uiShellMock.Object);

            var testSubject = CreateTestSubject(serviceOp);

            // Act
            using (new AssertIgnoreScope())
            {
                testSubject.Show(ValidToolWindowId);
            }

            // Non-success HResult so shouldn't have attempted to show the frame
            frameMock.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Show_FindWindowSucceededButReturnedNullToolWindow_NoException()
        {
            var uiShellMock = new Mock<IVsUIShell>();
            SetupFindToolWindow(uiShellMock, VSConstants.S_OK, ValidToolWindowId, toolWindowObject: null);

            var serviceOp = CreateServiceOperation(uiShellMock.Object);

            var testSubject = new ToolWindowService(serviceOp);

            Action act = () => testSubject.Show(ValidToolWindowId);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Show_FindWindowSucceededAndNonNullToolWindow_ToolWindowShown()
        {
            var uiShellMock = new Mock<IVsUIShell>();
            var frameMock = new Mock<IVsWindowFrame>();
            SetupFindToolWindow(uiShellMock, VSConstants.S_OK, ValidToolWindowId, frameMock.Object);

            var serviceOp = CreateServiceOperation(uiShellMock.Object);

            var testSubject = new ToolWindowService(serviceOp);

            // Act
            testSubject.Show(ValidToolWindowId);

            frameMock.Verify(x => x.Show(), Times.Once);
        }

        [TestMethod]
        [DataRow(VSConstants.S_OK)]
        [DataRow(VSConstants.E_FAIL)]
        [DataRow(VSConstants.E_OUTOFMEMORY)]
        public void EnsureToolWindowExists_VsServiceCalled(int vsServiceResult)
        {
            var uiShellMock = new Mock<IVsUIShell>();
            var frameMock = new Mock<IVsWindowFrame>();
            SetupFindToolWindow(uiShellMock, vsServiceResult, ValidToolWindowId, frameMock.Object);

            var serviceOp = CreateServiceOperation(uiShellMock.Object);

            var testSubject = CreateTestSubject(serviceOp);

            // Act
            using (new AssertIgnoreScope())
            {
                testSubject.EnsureToolWindowExists(ValidToolWindowId);
            }

            uiShellMock.VerifyAll();

            // Should never attempt to show the frame
            frameMock.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetToolWindow_ReturnsCorrectType()
        {
            var uiShellMock = new Mock<IVsUIShell>();

            object obj = new MyDummyToolWindow();
            var frameMock = new Mock<IVsWindowFrame>();
            frameMock.Setup(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out obj));

            SetupFindToolWindow(uiShellMock, VSConstants.S_OK, new Guid(MyDummyToolWindow.GuidAsString), frameMock.Object);

            var serviceOp = CreateServiceOperation<IMyDummyToolWindow>(uiShellMock.Object);


            var testSubject = CreateTestSubject(serviceOp);
            var result = testSubject.GetToolWindow<MyDummyToolWindow, IMyDummyToolWindow>();

            result.Should().BeSameAs(obj);
        }

        private interface IMyDummyToolWindow { }

        [Guid(GuidAsString)]
        private class MyDummyToolWindow : IMyDummyToolWindow
        {
            public const string GuidAsString = "45C7FC01-5569-4FE1-AB13-99F32B840D76";
        }

        private void SetupFindToolWindow(Mock<IVsUIShell> uiShell, int hrResult, Guid toolWindowId, IVsWindowFrame toolWindowObject)
        {
            uiShell
                .Setup(x => x.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowId, out toolWindowObject))
                .Returns(hrResult);
        }

        private IVsUIServiceOperation CreateServiceOperation(IVsUIShell svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<SVsUIShell, IVsUIShell>(It.IsAny<Action<IVsUIShell>>()))
                .Callback<Action<IVsUIShell>>(op => op(svcToPassToCallback));

            return serviceOp.Object;
        }

        private IVsUIServiceOperation CreateServiceOperation<V>(IVsUIShell svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<SVsUIShell, IVsUIShell, V>(It.IsAny<Func<IVsUIShell, V>>()))
                .Returns<Func<IVsUIShell, V>>(op => op(svcToPassToCallback));


            return serviceOp.Object;
        }

        private static ToolWindowService CreateTestSubject(IVsUIServiceOperation vsUIServiceOperation)
            => new(vsUIServiceOperation);
    }
}
