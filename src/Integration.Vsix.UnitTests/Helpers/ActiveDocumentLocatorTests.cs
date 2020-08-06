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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class ActiveDocumentLocatorTests
    {
        private Mock<IVsMonitorSelection> monitorSelectionMock;
        private Mock<ITextDocumentProvider> textDocumentProviderMock;
        private ITextDocument textDocument;

        private ActiveDocumentLocator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            monitorSelectionMock = new Mock<IVsMonitorSelection>();
            textDocumentProviderMock = new Mock<ITextDocumentProvider>();
            textDocument = Mock.Of<ITextDocument>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsShellMonitorSelection)))
                .Returns(monitorSelectionMock.Object);

            testSubject = new ActiveDocumentLocator(serviceProviderMock.Object, textDocumentProviderMock.Object);
        }

        [TestMethod]
        public void Find_NoOpenDocuments()
        {
            // Arrange
            Configure(activeFrame: null);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().BeNull();
            VerifyMonitorSelectionServiceCalled(null);
        }

        [TestMethod]
        public void Find_OpenDocument_NotAnIVsWindowFrame()
        {
            // Arrange
            var notAWindowFrame = new object();
            Configure(activeFrame: notAWindowFrame);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().BeNull();
            VerifyMonitorSelectionServiceCalled(notAWindowFrame);
        }

        [TestMethod]
        public void Find_OpenDocument_IVsWindowFrame_ReturnsDocument()
        {
            // Arrange
            var windowFrame = Mock.Of<IVsWindowFrame>();
            Configure(activeFrame: windowFrame);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().Be(textDocument);
            VerifyMonitorSelectionServiceCalled(windowFrame);
        }

        private void Configure(object activeFrame = null)
        {
            monitorSelectionMock.Setup(x => x.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out activeFrame));
            var vsFrame = activeFrame as IVsWindowFrame;
            textDocumentProviderMock.Setup(x => x.GetFromFrame(vsFrame)).Returns(textDocument);
        }

        private void VerifyMonitorSelectionServiceCalled(object obj) =>
            monitorSelectionMock.Verify(x => x.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out obj), Times.Once);
    }
}
