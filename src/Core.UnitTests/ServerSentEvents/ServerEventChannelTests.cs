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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.Core.UnitTests.ServerSentEvents
{
    [TestClass]
    public class ServerEventChannelTests
    {
        [TestMethod]
        public async Task Get_AfterPublish_ReturnsCorrectValue()
        {
            var testSubject = CreateTestSubject();
            var serverEvent = Mock.Of<IServerEvent>();

            testSubject.Publish(serverEvent);
            var receivedEvent = await testSubject.GetNextEventOrNullAsync();

            receivedEvent.Should().BeSameAs(serverEvent);
        }

        [TestMethod]
        public async Task Get_AwaitsForPublishBeforeReturning()
        {
            var testSubject = CreateTestSubject();
            var serverEvent = Mock.Of<IServerEvent>();

            var getTask =  testSubject.GetNextEventOrNullAsync();
            testSubject.Publish(serverEvent);
            var receivedEvent = await getTask;

            receivedEvent.Should().BeSameAs(serverEvent);
        }

        [TestMethod]
        public async Task Get_ReturnsMultiplePublishedItemsInCorrectOrder()
        {
            var testSubject = CreateTestSubject();
            var serverEvents = Enumerable.Range(0, 5).Select(_ => Mock.Of<IServerEvent>()).ToList();
            var receivedEvents = new List<IServerEvent>();

            foreach (var serverEvent in serverEvents)
            {
                testSubject.Publish(serverEvent);
            }
            for (var i = 0; i < serverEvents.Count; i++)
            {
                receivedEvents.Add(await testSubject.GetNextEventOrNullAsync());
            }

            receivedEvents.Should().BeEquivalentTo(serverEvents);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Get_AlreadyDisposedAndNoMoreItems_ReturnsNull(bool isEmptyChannel)
        {
            var testSubject = CreateTestSubject();
            var serverEvent = Mock.Of<IServerEvent>();

            if (!isEmptyChannel)
            {
                testSubject.Publish(serverEvent);
            }
            testSubject.Dispose();
            var receivedEvent = await testSubject.GetNextEventOrNullAsync();
            var receivedEvent2 = await testSubject.GetNextEventOrNullAsync();

            receivedEvent.Should().BeSameAs(isEmptyChannel ? null : serverEvent);
            receivedEvent2.Should().BeNull();
        }

        [TestMethod]
        public async Task Get_AfterDispose_ReturnsNull()
        {
            var testSubject = CreateTestSubject();

            testSubject.Dispose();
            var receivedEvent = await testSubject.GetNextEventOrNullAsync();

            receivedEvent.Should().BeNull();
        }

        [TestMethod]
        public async Task Get_DisposedWhileAwaiting_ReturnsNull()
        {
            var testSubject = CreateTestSubject();

            var getTask = testSubject.GetNextEventOrNullAsync();
            testSubject.Dispose();
            var receivedEvent = await getTask;

            receivedEvent.Should().BeNull();
        }

        [TestMethod]
        public void Publish_AlreadyDisposed_ThrowsObjectDisposedException()
        {
            var testSubject = CreateTestSubject();
            var serverEvent = Mock.Of<IServerEvent>();

            testSubject.Publish(serverEvent);
            testSubject.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => testSubject.Publish(serverEvent));
        }

        [TestMethod]
        public void Publish_WhenEventIsNull_ThrowsArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Assert.ThrowsException<ArgumentNullException>(() => testSubject.Publish(null));
        }

        [TestMethod]
        public void Dispose_IsIdempotent()
        {
            var testSubject = CreateTestSubject();
            var serverEvent = Mock.Of<IServerEvent>();

            testSubject.Dispose();
            testSubject.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() => testSubject.Publish(serverEvent));
        }

        private ServerEventChannel<IServerEvent> CreateTestSubject()
        {
            return new ServerEventChannel<IServerEvent>();
        }
    }
}
