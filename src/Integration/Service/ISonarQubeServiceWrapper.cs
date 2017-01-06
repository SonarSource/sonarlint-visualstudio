/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using SonarLint.VisualStudio.Integration.Service.DataModel;
using System;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// SonarQube service abstraction for testing purposes
    /// </summary>
    internal interface ISonarQubeServiceWrapper
    {
        /// <summary>
        /// Retrieves all the server projects
        /// </summary>
        bool TryGetProjects(ConnectionInformation serverConnection, CancellationToken token, out ProjectInformation[] serverProjects);

        /// <summary>
        /// Retrieves all the server properties.
        /// </summary>
        bool TryGetProperties(ConnectionInformation serverConnection, CancellationToken token, out ServerProperty[] properties);

        /// <summary>
        /// Retrieves the server's Roslyn Quality Profile export for the specified profile and language
        /// </summary>
        /// <remarks>
        /// The export contains everything required to configure the solution to match the SonarQube server analysis,
        /// including: the Code Analysis rule set, analyzer NuGet packages, and any other additional files for the analyzers.
        /// </remarks>
        /// <param name="profile">Quality profile. Required.</param>
        /// <param name="language">Language scope. Required.</param>
        bool TryGetExportProfile(ConnectionInformation serverConnection, QualityProfile profile, Language language, CancellationToken token, out RoslynExportProfile export);

        /// <summary>
        /// Retrieves all server plugins
        /// </summary>
        /// <returns>All server plugins, or null on connection failure</returns>
        bool TryGetPlugins(ConnectionInformation serverConnection, CancellationToken token, out ServerPlugin[] plugins);

        /// <summary>
        /// Retrieves the quality profile information for the specified project and language
        /// </summary>
        /// <param name="project">Required project information for which to retrieve the export</param>
        /// <param name="language">Language scope. Required.</param>
        bool TryGetQualityProfile(ConnectionInformation serverConnection, ProjectInformation project, Language language, CancellationToken token, out QualityProfile profile);

        /// <summary>
        /// Generate a <see cref="Uri"/> to the dashboard for the given project on the provided <paramref name="serverConnection"/>.
        /// </summary>
        /// <param name="project">Project to generate the <see cref="Uri"/> for</param>
        Uri CreateProjectDashboardUrl(ConnectionInformation serverConnection, ProjectInformation project);
    }
}
