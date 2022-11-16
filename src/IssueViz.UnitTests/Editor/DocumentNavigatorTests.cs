﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using ThreadHelper = SonarLint.VisualStudio.Integration.UnitTests.ThreadHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class DocumentNavigatorTests
    {
        private ITextView mockTextView;
        private SnapshotSpan mockSnapshotSpan;

        private Mock<IOutliningManagerService> outliningManagerServiceMock;
        private Mock<IEditorOperations> editorOperationsMock;
        private Mock<IThreadHandling> threadHandlingMock;

        private DocumentNavigator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            mockTextView = Mock.Of<ITextView>();

            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.Length).Returns(100);
            mockSnapshotSpan = new SnapshotSpan(mockSnapshot.Object, 1, 10);

            outliningManagerServiceMock = new Mock<IOutliningManagerService>();
            editorOperationsMock = new Mock<IEditorOperations>();

            var editorOperationsFactoryMock = new Mock<IEditorOperationsFactoryService>();
            editorOperationsFactoryMock
                .Setup(x => x.GetEditorOperations(mockTextView))
                .Returns(editorOperationsMock.Object);

            threadHandlingMock = new Mock<IThreadHandling>();
            threadHandlingMock.Setup(x => x.RunOnUIThread(It.IsAny<Action>())).Callback<Action>(op => op());

            testSubject = new DocumentNavigator(Mock.Of<IServiceProvider>(),
                Mock.Of<IVsEditorAdaptersFactoryService>(),
                outliningManagerServiceMock.Object,
                editorOperationsFactoryMock.Object,
                threadHandlingMock.Object
                );
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<DocumentNavigator, IDocumentNavigator>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IVsEditorAdaptersFactoryService>(),
                MefTestHelpers.CreateExport<IOutliningManagerService>(),
                MefTestHelpers.CreateExport<IEditorOperationsFactoryService>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void Navigate_NoOutliningManager_NoException()
        {
            outliningManagerServiceMock
                .Setup(x => x.GetOutliningManager(mockTextView))
                .Returns((IOutliningManager)null);

            Action act = () => testSubject.Navigate(mockTextView, mockSnapshotSpan);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Navigate_HasOutliningManager_RegionsExpanded()
        {
            var outliningManager = new Mock<IOutliningManager>();

            outliningManagerServiceMock
                .Setup(x => x.GetOutliningManager(mockTextView))
                .Returns(outliningManager.Object);

            testSubject.Navigate(mockTextView, mockSnapshotSpan);

            outliningManager.Verify(x =>
                    x.ExpandAll(mockSnapshotSpan, It.Is((Predicate<ICollapsed> p) => p(It.IsAny<ICollapsed>()))),
                Times.Once);

            outliningManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Navigate_SpanIsSelected()
        {
            testSubject.Navigate(mockTextView, mockSnapshotSpan);

            var expectedVirtualSnapshotSpan = new VirtualSnapshotSpan(mockSnapshotSpan);

            editorOperationsMock.Verify(x =>
                    x.SelectAndMoveCaret(It.Is((VirtualSnapshotPoint v) => v == expectedVirtualSnapshotSpan.Start),
                        It.Is((VirtualSnapshotPoint v) => v == expectedVirtualSnapshotSpan.End),
                        TextSelectionMode.Stream,
                        EnsureSpanVisibleOptions.AlwaysCenter),
                Times.Once);

            editorOperationsMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Navigate_VerifyIsRunningOnUIThread()
        {
            threadHandlingMock.Reset();
            threadHandlingMock.Setup(x => x.RunOnUIThread(It.IsAny<Action>())).Callback<Action>(op =>
            {
                outliningManagerServiceMock.Invocations.Count.Should().Be(0);
                editorOperationsMock.Invocations.Count.Should().Be(0);
                op();
                outliningManagerServiceMock.Invocations.Count.Should().Be(1);
                editorOperationsMock.Invocations.Count.Should().Be(1);
            });

            testSubject.Navigate(mockTextView, mockSnapshotSpan);

            threadHandlingMock.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);
        }
    }
}
