using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Issues;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.TaintVulnerabilities;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class ServerSentEventSessionManagerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerSentEventSessionManager, IServerSentEventSessionManager>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<ITaintServerEventSourcePublisher>(),
                MefTestHelpers.CreateExport<IIssueChangedServerEventSourcePublisher>());
        }
    }
}
