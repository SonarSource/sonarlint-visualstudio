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
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.SharedUI;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.TaintList
{
    [TestClass]
    public class TaintToolWindowTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            // TaintViewModel needs to be created on the UI thread
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

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
            var componentModelMock = new Mock<IComponentModel>();

            var store = new Mock<ITaintStore>();
            store.Setup(x => x.GetAll())
                .Returns(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(
                    new ObservableCollection<IAnalysisIssueVisualization>()));

            AddMefService(componentModelMock, store.Object);
            AddMefService<ILocationNavigator>(componentModelMock);
            AddMefService<IActiveDocumentTracker>(componentModelMock);
            AddMefService<IActiveDocumentLocator>(componentModelMock);
            AddMefService<IShowInBrowserService>(componentModelMock);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SComponentModel)))
                .Returns(componentModelMock.Object);

            frameMock ??= new Mock<IVsWindowFrame>();

            return new TaintToolWindow(serviceProviderMock.Object) { Frame = frameMock.Object };
        }

        private static void AddMefService<T>(Mock<IComponentModel> componentModel, T instance = null) where T: class
        {
            instance ??= Mock.Of<T>();
            componentModel.Setup(x => x.GetService<T>()).Returns(instance);
        }
    }
}
