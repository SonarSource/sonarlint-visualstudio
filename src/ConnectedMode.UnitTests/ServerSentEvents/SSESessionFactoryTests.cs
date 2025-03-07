﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Issue;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.QualityProfile;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents;

[TestClass]
public class SSESessionFactoryTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SSESessionFactory, ISSESessionFactory>(
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IIssueServerEventSourcePublisher>(),
            MefTestHelpers.CreateExport<IQualityProfileServerEventSourcePublisher>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void Create_ReturnsCorrectType()
    {
        var testSubject = CreateTestSubject();

        var sseSession = testSubject.Create("MyProjectName", null);

        sseSession.Should().NotBeNull().And.BeOfType<SSESessionFactory.SSESession>();
    }

    [TestMethod]
    public void Create_AfterDispose_Throws()
    {
        var testSubject = CreateTestSubject();

        testSubject.Dispose();
        Action act = () => testSubject.Create("MyProjectName", null);

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void Dispose_IdempotentAndDisposesPublishers()
    {
        var issuesPublisherMock = new Mock<IIssueServerEventSourcePublisher>();
        var qualityProfilePublisherMock = new Mock<IQualityProfileServerEventSourcePublisher>();
        var testSubject = CreateTestSubject(issuesPublisherMock, qualityProfilePublisherMock);

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        issuesPublisherMock.Verify(p => p.Dispose(), Times.Once);
        qualityProfilePublisherMock.Verify(p => p.Dispose(), Times.Once);
    }

    private SSESessionFactory CreateTestSubject(Mock<IIssueServerEventSourcePublisher> issuePublisher = null,
        Mock<IQualityProfileServerEventSourcePublisher> qualityProfileServerEventSourcePublisher = null) =>
        new(Mock.Of<ISonarQubeService>(),
            issuePublisher?.Object ?? Mock.Of<IIssueServerEventSourcePublisher>(),
            qualityProfileServerEventSourcePublisher?.Object ?? Mock.Of<IQualityProfileServerEventSourcePublisher>(),
            Mock.Of<IThreadHandling>(),
            Mock.Of<ILogger>());
}
