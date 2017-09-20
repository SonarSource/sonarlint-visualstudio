/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Service;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SanityTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableTelemetryLogger logger;

        [TestInitialize]
        public void TestInitialize()
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

        [TestCleanup]
        public void TestCleanup()
        {
            this.logger.DumpAllToOutput();
        }

        [TestMethod]
        [Ignore] // This should be an IT + targeting sonarqube.com seems wrong
        [Description("Use the live SQ server to verify that the API we use are still supported."
            + "The assumptions are:"
            + "(a) the site is alive "
            + "(b) the site has the latest version of SQ installed"
            + "(c) configured for anonymous access"
            + "(d) the site has at least one project (with associated rules)")]
        public void LatestServer_APICompatibility()
        {
            // Arrange the service used to interact with SQ
            var s = new SonarQubeServiceWrapper(this.serviceProvider);
            var connection = new ConnectionInformation(new Uri("https://sonarqube.com"));

            // Step 1: Connect anonymously
            SonarQubeProject[] projects = null;

            RetryAction(() => s.TryGetProjects(connection, CancellationToken.None, out projects),
                                        "Get projects from SonarQube server");
            projects.Should().NotBeEmpty("No projects were returned");

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

            FluentAssertions.Execution.Execute.Assertion.FailWith("Failed executing the action (with retries): {0}", description);
        }
    }
}