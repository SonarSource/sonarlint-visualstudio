/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.QuickInfo
{
    [TestClass]
    public class SonarLintQuickInfoControllerTests
    {
        private const int snapshotPosition = 123;
        private const int pointPosition = 12;

        private ITextView textView;
        private SonarLintQuickInfoControllerProvider provider;
        private Mock<ITextView> textViewMock;
        private Mock<ITextSnapshot> textSnapshotMock;
        private Mock<ITrackingPoint> trackingPointMock;
        private Mock<IBufferGraph> bufferGraphMock;
        private Mock<IQuickInfoBroker> quickInfoBrokerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            trackingPointMock = new Mock<ITrackingPoint>();

            textSnapshotMock = new Mock<ITextSnapshot>(MockBehavior.Strict);
            textSnapshotMock.SetupGet(x => x.Length).Returns(1000);

            bufferGraphMock = new Mock<IBufferGraph>(MockBehavior.Strict);

            textViewMock = new Mock<ITextView>(MockBehavior.Strict);
            textViewMock.SetupGet(x => x.BufferGraph).Returns(bufferGraphMock.Object);
            textViewMock.SetupGet(x => x.TextSnapshot).Returns(textSnapshotMock.Object);

            textView = textViewMock.Object;

            quickInfoBrokerMock = new Mock<IQuickInfoBroker>(MockBehavior.Strict);

            provider = new SonarLintQuickInfoControllerProvider
            {
                QuickInfoBroker = quickInfoBrokerMock.Object,
            };
        }

        [TestMethod]
        public void TextView_MouseHover_TriggersQuickInfo()
        {
            // Arrange
            textSnapshotMock
                .Setup(x => x.CreateTrackingPoint(pointPosition, PointTrackingMode.Positive))
                .Returns(trackingPointMock.Object);

            bufferGraphMock
                .Setup(x => x.MapDownToFirstMatch(It.IsAny<SnapshotPoint>(), PointTrackingMode.Positive,
                    It.IsAny<Predicate<ITextSnapshot>>(), PositionAffinity.Predecessor))
                .Returns(new SnapshotPoint(textSnapshotMock.Object, pointPosition));

            quickInfoBrokerMock
                .Setup(x => x.IsQuickInfoActive(textView))
                .Returns(false);

            quickInfoBrokerMock
                .Setup(x => x.TriggerQuickInfo(textView, trackingPointMock.Object, true))
                .Returns((IQuickInfoSession)null);

            var controller = new SonarLintQuickInfoController(textView, new List<ITextBuffer>(), provider);

            // Act
            textViewMock.Raise(x => x.MouseHover += null,
                new MouseHoverEventArgs(textView, snapshotPosition, new Mock<IMappingPoint>().Object));

            // Assert
            Mock.VerifyAll(textSnapshotMock, bufferGraphMock, textViewMock, quickInfoBrokerMock);
        }

        [TestMethod]
        public void TextView_MouseHover_QuickInfoActive_True()
        {
            // Arrange
            bufferGraphMock
                .Setup(x => x.MapDownToFirstMatch(It.IsAny<SnapshotPoint>(), PointTrackingMode.Positive,
                    It.IsAny<Predicate<ITextSnapshot>>(), PositionAffinity.Predecessor))
                .Returns(new SnapshotPoint(textSnapshotMock.Object, pointPosition));

            var textView = textViewMock.Object;

            quickInfoBrokerMock
                .Setup(x => x.IsQuickInfoActive(textView))
                .Returns(true);

            var controller = new SonarLintQuickInfoController(textView, new List<ITextBuffer>(), provider);

            // Act
            textViewMock.Raise(x => x.MouseHover += null,
                new MouseHoverEventArgs(textView, snapshotPosition, new Mock<IMappingPoint>().Object));

            // Assert
            Mock.VerifyAll(textSnapshotMock, bufferGraphMock, textViewMock, quickInfoBrokerMock);
        }

        [TestMethod]
        public void TextView_MouseHover_No_SnapshotPoint()
        {
            // Arrange
            bufferGraphMock
                .Setup(x => x.MapDownToFirstMatch(It.IsAny<SnapshotPoint>(), PointTrackingMode.Positive,
                    It.IsAny<Predicate<ITextSnapshot>>(), PositionAffinity.Predecessor))
                .Returns((SnapshotPoint?)null);

            var controller = new SonarLintQuickInfoController(textView, new List<ITextBuffer>(), provider);

            // Act
            textViewMock.Raise(x => x.MouseHover += null,
                new MouseHoverEventArgs(textView, snapshotPosition, new Mock<IMappingPoint>().Object));

            // Assert
            Mock.VerifyAll(textSnapshotMock, bufferGraphMock, textViewMock, quickInfoBrokerMock);
        }

        [TestMethod]
        public void Detach_Turns_Off_EventHandling()
        {
            // Arrange
            var controller = new SonarLintQuickInfoController(textViewMock.Object, new List<ITextBuffer>(), provider);

            // Act
            controller.Detach(textViewMock.Object);

            // Assert
            Mock.VerifyAll(quickInfoBrokerMock); // Do not verify textViewMock, the setup properties are not called

            // nothing really to verify here
        }

        [TestMethod]
        public void ConnectSubjectBuffer_Adds_Buffer()
        {
            // Arrange
            var buffers = new List<ITextBuffer>();

            var controller = new SonarLintQuickInfoController(
                new Mock<ITextView>().Object, buffers, new SonarLintQuickInfoControllerProvider());

            // Act
            controller.ConnectSubjectBuffer(new Mock<ITextBuffer>().Object);

            // Assert
            buffers.Should().HaveCount(1);
        }

        [TestMethod]
        public void DisconnectSubjectBuffer_Removes_Buffer()
        {
            // Arrange
            var subjectBuffer = new Mock<ITextBuffer>().Object;

            var buffers = new List<ITextBuffer>
            {
                subjectBuffer
            };

            var controller = new SonarLintQuickInfoController(
                new Mock<ITextView>().Object, buffers, new SonarLintQuickInfoControllerProvider());

            // Act
            controller.DisconnectSubjectBuffer(subjectBuffer);

            // Assert
            buffers.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_Argument_Checks()
        {
            Action action = () => new SonarLintQuickInfoController(null, new List<ITextBuffer>(), new SonarLintQuickInfoControllerProvider());
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("textView");

            action = () => new SonarLintQuickInfoController(new Mock<ITextView>().Object, null, new SonarLintQuickInfoControllerProvider());
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("subjectBuffers");

            action = () => new SonarLintQuickInfoController(new Mock<ITextView>().Object, new List<ITextBuffer>(), null);
            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("provider");
        }
    }
}
