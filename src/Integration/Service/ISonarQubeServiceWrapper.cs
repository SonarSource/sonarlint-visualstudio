//-----------------------------------------------------------------------
// <copyright file="ISonarQubeServiceWrapper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// SonarQube service abstraction for testing purposes
    /// </summary>
    internal interface ISonarQubeServiceWrapper
    {
        /// <summary>
        /// When connected this property will contain the connection information, null otherwise.
        /// </summary>
        ConnectionInformation CurrentConnection { get; }

        /// <summary>
        /// Connects using the provided information.
        /// If the connection is successful <see cref="CurrentConnection"/> will be set and the project information will be returned.
        /// </summary>
        /// <param name="connectionInformation">Required connecting information</param>
        IEnumerable<ProjectInformation> Connect(ConnectionInformation connectionInformation, CancellationToken token);

        /// <summary>
        /// Disconnects from the <see cref="CurrentConnection"/>
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Retrieves the Roslyn Quality Profile export for the specified project using the <see cref="CurrentConnection"/>.
        /// </summary>
        /// <remarks>
        /// The export contains everything required to configure the solution to match the SonarQube server analysis,
        /// including: the Code Analysis rule set, analyzer NuGet packages, and any other additional files for the analyzers.
        /// </remarks>
        /// <param name="project">Required project information for which to retrieve the export</param>
        /// <param name="language">Language scope. Required.</param>
        RoslynExportProfile GetExportProfile(ProjectInformation project, string language, CancellationToken token);

        /// <summary>
        /// Retrieves the version of the specified server plugin based on its <paramref name="pluginKey"/>.
        /// </summary>
        /// <param name="pluginKey">Key of the server plugin</param>
        /// <returns>The version string of the plugin, or null if it is not installed</returns>
        string GetPluginVersion(string pluginKey, CancellationToken token);
    }
}
