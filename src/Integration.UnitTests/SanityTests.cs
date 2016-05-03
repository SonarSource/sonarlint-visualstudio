//-----------------------------------------------------------------------
// <copyright file="SanityTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Linq;
using System.Threading;

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
        [Description("Use the live SQ server to verify that the API we use are still supported."
            + "The assumptions are:"
            + "(a) the site is alive "
            + "(b) the site has the latest version of SQ installed"
            + "(c) configured for anonymous access"
            + "(d) the site has has at least one project (with associated rules)")]
        public void LatestServer_APICompatibility()
        {
            // Setup the service used to interact with SQ
            var s = new SonarQubeServiceWrapper(this.serviceProvider);
            var connection = new ConnectionInformation(new Uri("http://nemo.sonarqube.org"));

            // Step 1: Connect anonymously
            ProjectInformation[] projects = null;

            RetryAction(() => s.TryGetProjects(connection, CancellationToken.None, out projects),
                                        "Get projects from SonarQube server");
            Assert.AreNotEqual(0, projects.Length, "No projects were returned");

            // Step 2: Get quality profile for the first project
            var project = projects.FirstOrDefault();
            QualityProfile profile = null;
            RetryAction(() => s.TryGetQualityProfile(connection, project, Language.CSharp, CancellationToken.None, out profile),
                                        "Get quality profile from SonarQube server");
            Assert.IsNotNull(profile, "No quality profile was returned");

            // Step 3: Get quality profile export for the quality profile
            RoslynExportProfile export = null;
            RetryAction(() => s.TryGetExportProfile(connection, profile, Language.CSharp, CancellationToken.None, out export),
                                        "Get quality profile export from SonarQube server");
            Assert.IsNotNull(export, "No quality profile export was returned");

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

            Assert.Fail("Failed executing the action (with retries): {0}", description);
        }
    }
}
