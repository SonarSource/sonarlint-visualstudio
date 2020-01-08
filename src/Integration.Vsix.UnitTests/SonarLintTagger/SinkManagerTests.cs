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
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SinkManagerTests
    {
        [TestMethod]
        public void CallsToSink_AddFactory_NonCriticalException_Suppressed()
        {
            // Arrange
            var mockRegister = new Mock<ISinkManagerRegister>();
            var mockSink = new Mock<ITableDataSink>();

            var sinkManager = new SinkManager(mockRegister.Object, mockSink.Object);
            mockSink.Setup(x => x.AddFactory(It.IsAny<ITableEntriesSnapshotFactory>(), false))
                .Throws(new InvalidCastException("add factory custom error"));

            // Act
            sinkManager.AddFactory(CreateSnapshotFactory());

            // Assert
            mockSink.Verify(x => x.AddFactory(It.IsAny<ITableEntriesSnapshotFactory>(), false), Times.Once);
        }

        [TestMethod]
        public void CallsToSink_RemoveFactory_NonCriticalException_Suppressed()
        {
            // Arrange
            var mockRegister = new Mock<ISinkManagerRegister>();
            var mockSink = new Mock<ITableDataSink>();

            var sinkManager = new SinkManager(mockRegister.Object, mockSink.Object);
            mockSink.Setup(x => x.RemoveFactory(It.IsAny<ITableEntriesSnapshotFactory>()))
                .Throws(new InvalidCastException("remove factory custom error"));

            // Act
            sinkManager.RemoveFactory(CreateSnapshotFactory());

            // Assert
            mockSink.Verify(x => x.RemoveFactory(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Once);
        }

        [TestMethod]
        public void CallsToSink_UpdateSink_NonCriticalException_Suppressed()
        {
            // Arrange
            var mockRegister = new Mock<ISinkManagerRegister>();
            var mockSink = new Mock<ITableDataSink>();

            var sinkManager = new SinkManager(mockRegister.Object, mockSink.Object);
            mockSink.Setup(x => x.FactorySnapshotChanged(null))
                .Throws(new InvalidCastException("update custom error"));

            // Act
            sinkManager.UpdateSink();

            // Assert
            mockSink.Verify(x => x.FactorySnapshotChanged(null), Times.Once);
        }

        [TestMethod]
        public void CallsToSink_AddFactory_CriticalException_NotSuppressed()
        {
            // Arrange
            var mockRegister = new Mock<ISinkManagerRegister>();
            var mockSink = new Mock<ITableDataSink>();

            var sinkManager = new SinkManager(mockRegister.Object, mockSink.Object);
            mockSink.Setup(x => x.AddFactory(It.IsAny<ITableEntriesSnapshotFactory>(), false))
                .Throws(new StackOverflowException("add factory custom error"));

            // Act & assert
            Action act = () => sinkManager.AddFactory(CreateSnapshotFactory());
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("add factory custom error");
        }

        [TestMethod]
        public void CallsToSink_RemoveFactory_CriticalException_NotSuppressed()
        {
            // Arrange
            var mockRegister = new Mock<ISinkManagerRegister>();
            var mockSink = new Mock<ITableDataSink>();

            var sinkManager = new SinkManager(mockRegister.Object, mockSink.Object);
            mockSink.Setup(x => x.RemoveFactory(It.IsAny<ITableEntriesSnapshotFactory>()))
                .Throws(new StackOverflowException("remove factory custom error"));

            // Act & assert
            Action act = () => sinkManager.RemoveFactory(CreateSnapshotFactory());
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("remove factory custom error");
        }

        [TestMethod]
        public void CallsToSink_Update_CriticalException_NotSuppressed()
        {
            // Arrange
            var mockRegister = new Mock<ISinkManagerRegister>();
            var mockSink = new Mock<ITableDataSink>();

            var sinkManager = new SinkManager(mockRegister.Object, mockSink.Object);
            mockSink.Setup(x => x.FactorySnapshotChanged(null))
                .Throws(new StackOverflowException("update custom error"));

            // Act & assert
            Action act = () => sinkManager.UpdateSink();
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("update custom error");
        }

        [TestMethod]
        public void RegisterAndUnregister()
        {
            // Arrange
            var mockRegister = new Mock<ISinkManagerRegister>();
            var mockSink = new Mock<ITableDataSink>();

            // 1. Create -> should register self
            var sinkManager = new SinkManager(mockRegister.Object, mockSink.Object);
            mockRegister.Verify(x => x.AddSinkManager(sinkManager), Times.Once);
            mockRegister.Verify(x => x.RemoveSinkManager(It.IsAny<SinkManager>()), Times.Never);

            // 2. Dispose -> should unregister self
            sinkManager.Dispose();
            mockRegister.Verify(x => x.AddSinkManager(sinkManager), Times.Once);
            mockRegister.Verify(x => x.RemoveSinkManager(sinkManager), Times.Once);

            // 3. Another Dispose -> should be a no-op
            sinkManager.Dispose();
            mockRegister.Verify(x => x.AddSinkManager(sinkManager), Times.Once);
            mockRegister.Verify(x => x.RemoveSinkManager(sinkManager), Times.Once);
        }

        private static SnapshotFactory CreateSnapshotFactory() =>
            new SnapshotFactory(new IssuesSnapshot("proj", "filePath", 1, new IssueMarker[] { }));
    }
}
