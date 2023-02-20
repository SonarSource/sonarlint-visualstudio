﻿/*
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


using System;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents
{
    [TestClass]
    public class SSESessionFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SSESessionFactory, ISSESessionFactory>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<ITaintServerEventSourcePublisher>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void Create_ReturnsCorrectType()
        {
            var testSubject = CreateTestSubject();

            var sseSession = testSubject.Create("MyProjectName");

            sseSession.Should().NotBeNull().And.BeOfType<SSESessionFactory.SSESession>();
        }

        [TestMethod]
        public void Create_AfterDispose_Throws()
        {
            var testSubject = CreateTestSubject();

            testSubject.Dispose();
            Action act = () => testSubject.Create("MyProjectName");

            act.Should().Throw<ObjectDisposedException>();
        }

        [TestMethod]
        public void Dispose_IdempotentAndDisposesPublishers()
        {
            var taintPublisherMock = new Mock<ITaintServerEventSourcePublisher>();
            var testSubject = CreateTestSubject(taintPublisherMock);

            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            taintPublisherMock.Verify(p => p.Dispose(), Times.Once);
        }

        private SSESessionFactory CreateTestSubject(Mock<ITaintServerEventSourcePublisher> taintPublisher = null)
        {
            return new SSESessionFactory(Mock.Of<ISonarQubeService>(),
                taintPublisher ?.Object ?? Mock.Of<ITaintServerEventSourcePublisher>(),
                Mock.Of<IThreadHandling>());
        }
    }
}
