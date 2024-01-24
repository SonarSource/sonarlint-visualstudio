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

using System;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
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
            monitorSelectionMock = new Mock<IVsMonitorSelection>();
            textDocumentProviderMock = new Mock<ITextDocumentProvider>();
            textDocument = Mock.Of<ITextDocument>();

            var vsServiceOperation = CreateServiceOperation(monitorSelectionMock.Object);

            testSubject = new ActiveDocumentLocator(vsServiceOperation, textDocumentProviderMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<ActiveDocumentLocator, IActiveDocumentLocator>(
                MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
                MefTestHelpers.CreateExport<ITextDocumentProvider>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<ActiveDocumentLocator>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();
            var textDocProvider = new Mock<ITextDocumentProvider>();

            _ = new ActiveDocumentLocator(serviceOp.Object, textDocProvider.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceOp.Invocations.Should().BeEmpty();
            textDocProvider.Invocations.Should().BeEmpty();
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

        private IVsUIServiceOperation CreateServiceOperation(IVsMonitorSelection svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<SVsShellMonitorSelection, IVsMonitorSelection, ITextDocument>(It.IsAny<Func<IVsMonitorSelection, ITextDocument>>()))
                .Returns<Func<IVsMonitorSelection, ITextDocument>>(op => op(svcToPassToCallback));


            return serviceOp.Object;
        }
    }
}
