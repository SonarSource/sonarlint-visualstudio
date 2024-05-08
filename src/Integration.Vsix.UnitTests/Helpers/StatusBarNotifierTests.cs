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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class StatusBarNotifierTests
    {
        private object statusIcon = (short) Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;

        [TestMethod]
        public void MefCtor_CheckIsExported()
         => MefTestHelpers.CheckTypeCanBeImported<StatusBarNotifier, IStatusBarNotifier>(
             MefTestHelpers.CreateExport<IVsUIServiceOperation>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<StatusBarNotifier>();

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
        [DataRow(true)]
        [DataRow(false)]
        public void Notify_DisplaysMessageAndSpinner(bool showSpinner)
        {
            var expectedMessage = Guid.NewGuid().ToString();

            var statusBar = new Mock<IVsStatusbar>();
            var serviceOp = CreateServiceOperation(statusBar.Object);
            var testSubject = CreateTestSubject(serviceOp);
            testSubject.Notify(expectedMessage, showSpinner);

            statusBar.Verify(x => x.SetText(expectedMessage), Times.Once);
            statusBar.Verify(x => x.Animation(showSpinner ? 1 : 0, ref statusIcon), Times.Once);
            statusBar.VerifyNoOtherCalls();
        }

        private IVsUIServiceOperation CreateServiceOperation(IVsStatusbar svcToPassToCallback, Action callback = null)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<IVsStatusbar, IVsStatusbar>(It.IsAny<Action<IVsStatusbar>>()))
                .Callback<Action<IVsStatusbar>>(op => {
                    callback?.Invoke();
                    op(svcToPassToCallback);
                    });

            return serviceOp.Object;
        }

        private static StatusBarNotifier CreateTestSubject(IVsUIServiceOperation vsUIServiceOperation)
        {
            var testSubject = new StatusBarNotifier(vsUIServiceOperation);
            return testSubject;
        }
    }
}
