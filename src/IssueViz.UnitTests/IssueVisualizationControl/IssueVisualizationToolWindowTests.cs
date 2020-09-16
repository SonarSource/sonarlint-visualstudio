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
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class IssueVisualizationToolWindowTests
    {
        [TestMethod]
        public void Dispose_FrameIsClosed()
        {
            var frameMock = new Mock<IVsWindowFrame>();
            
            var testSubject = CreateTestSubject();
            testSubject.Frame = frameMock.Object;

            testSubject.Dispose();

            frameMock.Verify(x=> x.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave), Times.Once());
        }

        private IssueVisualizationToolWindow CreateTestSubject()
        {
            var componentModelMock = new Mock<IComponentModel>();
            RegisterDummyMefService<IAnalysisIssueSelectionService>(componentModelMock);
            RegisterDummyMefService<ILocationNavigator>(componentModelMock);
            RegisterDummyMefService<ILogger>(componentModelMock);

            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(SComponentModel)))
                .Returns(componentModelMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsImageService)))
                .Returns(Mock.Of<IVsImageService2>());

            return new IssueVisualizationToolWindow(serviceProviderMock.Object);
        }

        private void RegisterDummyMefService<T>(Mock<IComponentModel> componentModelMock) 
            where T : class
        {
            componentModelMock
                .Setup(x => x.GetService<T>())
                .Returns(Mock.Of<T>());
        }
    }
}
