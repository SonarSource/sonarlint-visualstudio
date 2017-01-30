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

using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
 using Xunit;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class SanityTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableTelemetryLogger logger;

        public SanityTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.logger = new ConfigurableTelemetryLogger();
            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(
                    MefTestHelpers.CreateExport<ITelemetryLogger>(this.logger)));
        }
        [Fact]
        public void LatestServer_APICompatibility()
        {
            // Arrange the service used to interact with SQ
            var s = new SonarQubeServiceWrapper(this.serviceProvider);
            var connection = new ConnectionInformation(new Uri("https://sonarqube.com"));

            // Step 1: Connect anonymously
            ProjectInformation[] projects = null;

            RetryAction(() => s.TryGetProjects(connection, CancellationToken.None, out projects),
                                        "Get projects from SonarQube server");
            projects.Length.Should().NotBe(0, "No projects were returned");

            // Step 2: Get quality profile for the first project
            var project = projects.FirstOrDefault();
            QualityProfile profile = null;
            RetryAction(() => s.TryGetQualityProfile(connection, project, Language.CSharp, CancellationToken.None, out profile),
                                        "Get quality profile from SonarQube server");
            profile.Should().NotBeNull("No quality profile was returned");

            // Step 3: Get quality profile export for the quality profile
            RoslynExportProfile export = null;
            RetryAction(() => s.TryGetExportProfile(connection, profile, Language.CSharp, CancellationToken.None, out export),
                                        "Get quality profile export from SonarQube server");
            export.Should().NotBeNull("No quality profile export was returned");

            // Errors are logged to output window pane and we don't expect any
            this.outputWindowPane.AssertOutputStrings(0);
        }

        private static void RetryAction(Func<bool> action, string description, int maxAttempts = 3)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (!action())
                {
                    Thread.Sleep(100);
                }
                else
                {
                    return;
                }
            }

            true.Should().BeFalse("Failed executing the action (with retries): {0}", description);
        }
    }
}
