using System;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ServerSentEvents.Issues;
using SonarLint.VisualStudio.Core.ServerSentEvents.TaintVulnerabilities;
using SonarLint.VisualStudio.Integration.UnitTests;
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
                MefTestHelpers.CreateExport<IIssueChangedServerEventSourcePublisher>(),
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
            var issuesPublisherMock = new Mock<IIssueChangedServerEventSourcePublisher>();
            var taintPublisherMock = new Mock<ITaintServerEventSourcePublisher>();
            var testSubject = CreateTestSubject(issuesPublisherMock, taintPublisherMock);

            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            issuesPublisherMock.Verify(p => p.Dispose(), Times.Once);
            taintPublisherMock.Verify(p => p.Dispose(), Times.Once);
        }

        private SSESessionFactory CreateTestSubject(Mock<IIssueChangedServerEventSourcePublisher> issuesPublisher = null, Mock<ITaintServerEventSourcePublisher> taintPublisher = null)
        {
            return new SSESessionFactory(Mock.Of<ISonarQubeService>(),
                issuesPublisher?.Object ?? Mock.Of<IIssueChangedServerEventSourcePublisher>(),
                taintPublisher ?.Object ?? Mock.Of<ITaintServerEventSourcePublisher>(),
                Mock.Of<IThreadHandling>());
        }
    }
}
