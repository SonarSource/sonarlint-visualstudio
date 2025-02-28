using System.ComponentModel.Composition;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.SonarQubeClient
{
    /// <summary>
    /// Contains version specific feature configuration
    /// </summary>
    public interface IConnectedModeFeaturesConfiguration
    {
        /// <summary>
        /// Indicates whether Local Hotspot Analysis is supported in the current Connected Mode state
        /// </summary>
        /// <returns>True if connected to SCloud or SQube 9.7 and above, False otherwise</returns>
        bool IsHotspotsAnalysisEnabled();

        /// <summary>
        /// Indicates whether the new Clean Code Taxonomy should be used in the current Connected Mode state
        /// </summary>
        /// <returns>False if connected to SQube 10.1.X and below, True otherwise (including Standalone)</returns>
        bool IsNewCctAvailable();
    }

    [Export(typeof(IConnectedModeFeaturesConfiguration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ConnectedModeFeaturesConfiguration : IConnectedModeFeaturesConfiguration
    {
        private readonly Version minimalSonarQubeVersionForHotspots = new Version(9, 7);
        private readonly Version minimalSonarQubeVersionForNewTaxonomy = new Version(10, 2);
        private readonly Version minimalSonarQubeVersionForAccept = new Version(10, 4);
        private readonly ISonarQubeService sonarQubeService;

        [ImportingConstructor]
        public ConnectedModeFeaturesConfiguration(ISonarQubeService sonarQubeService)
        {
            this.sonarQubeService = sonarQubeService;
        }

        public bool IsNewCctAvailable()
        {
            var serverInfo = sonarQubeService.GetServerInfo();

            // use new cct in standalone, connected to SC or connected to SQ >=10.2
            return serverInfo == null || IsSupportedForVersion(serverInfo, minimalSonarQubeVersionForNewTaxonomy);
        }

        public bool IsHotspotsAnalysisEnabled()
        {
            var serverInfo = sonarQubeService.GetServerInfo();

            // analyze hotspots connected to SC or connected to SQ >= 9.7
            return serverInfo != null && IsSupportedForVersion(serverInfo, minimalSonarQubeVersionForHotspots);
        }

        private static bool IsSupportedForVersion(ServerInfo serverInfo, Version minimumVersion) =>
            serverInfo.ServerType == ServerType.SonarCloud
            || serverInfo.ServerType == ServerType.SonarQube &&
            serverInfo.Version >= minimumVersion;

        public bool IsAcceptTransitionAvailable()
        {
            var serverInfo = sonarQubeService.GetServerInfo();

            return serverInfo != null && IsSupportedForVersion(serverInfo, minimalSonarQubeVersionForAccept);
        }
    }
}
