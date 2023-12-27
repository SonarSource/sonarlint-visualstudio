/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests
{
    [TestClass]
    public class SLCoreListenerSetUpTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SLCoreListenerSetUp, ISLCoreListenerSetUp>();
        }

        [TestMethod]
        public void Setup_NoListenerRegistered_DoesNotCallRpcWrapper()
        {
            var wrapperMock = new Mock<ISLCoreJsonRpc>();

            var listeners = new ISLCoreListener[] { };

            var testSubject = new SLCoreListenerSetUp(listeners);

            testSubject.Setup(wrapperMock.Object);

            wrapperMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Setup_ListenersRegistered_CallRpcWrapper()
        {
            var wrapperMock = new Mock<ISLCoreJsonRpc>();

            var listener0 = new Mock<ISLCoreListener>();
            var listener1 = new Mock<TestListener1>();
            var listener2 = new Mock<TestListener2>();

            var listeners = new ISLCoreListener[] { listener0.Object, listener1.Object, listener2.Object };

            var testSubject = new SLCoreListenerSetUp(listeners);

            testSubject.Setup(wrapperMock.Object);

            wrapperMock.Verify(w => w.AttachListener(listener0.Object), Times.Once);
            wrapperMock.Verify(w => w.AttachListener(listener1.Object), Times.Once);
            wrapperMock.Verify(w => w.AttachListener(listener2.Object), Times.Once);
            wrapperMock.VerifyNoOtherCalls();
        }

        public interface TestListener1 : ISLCoreListener { }

        public interface TestListener2 : ISLCoreListener { }
    }
}
