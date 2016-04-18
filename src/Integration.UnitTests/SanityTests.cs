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
        private ConfigurableVsGeneralOutputWindowPane outputWindowPane;
        private ConfigurableTelemetryLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindowPane = new ConfigurableVsGeneralOutputWindowPane());
            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(
                    MefTestHelpers.CreateExport<ITelemetryLogger>(this.logger = new ConfigurableTelemetryLogger())));
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
        [Ignore]
        public void LatestServer_APICompatibility()
        {
            // Setup the service used to interact with SQ
            var s = new SonarQubeServiceWrapper(this.serviceProvider);

            // Step 1: Connect anonymously
            var projects = RetryAction(() => s.Connect(new ConnectionInformation(new Uri("http://nemo.sonarqube.org")), CancellationToken.None),
                                        "Get projects from SonarQube server");
            Assert.AreNotEqual(0, projects.Count(), "No projects were returned");

            // Step 2: Get quality profile export for the first project
            var project = projects.FirstOrDefault();
            var export = RetryAction(() => s.GetExportProfile(project, SonarQubeServiceWrapper.CSharpLanguage, CancellationToken.None),
                                        "Get quality profile export from SonarQube server");
            Assert.IsNotNull(export, "No quality profile export was returned");

            // Errors are logged to output window pane and we don't expect any
            this.outputWindowPane.AssertOutputStrings(0);
        }

        private static T RetryAction<T>(Func<T> action, string description, int maxAttempts = 3)
            where T: class
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                T value = action();
                if (value == null)
                {
                    Thread.Sleep(100);
                }
                else
                { 
                    return value;
                }
            }

            Assert.Fail("Failed executing the action (with retries): {0}", description);
            return null;
        }
    }
}
