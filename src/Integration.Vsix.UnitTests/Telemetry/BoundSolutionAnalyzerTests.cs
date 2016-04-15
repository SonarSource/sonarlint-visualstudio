//-----------------------------------------------------------------------
// <copyright file="BoundSolutionAnalyzerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BoundSolutionAnalyzerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsMonitorSelection monitorSelection;
        private ConfigurableTelemetryLogger logger;
        private DTEMock dte;
        private string solutionRootFolder;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.monitorSelection = KnownUIContextsAccessor.MonitorSelectionService;

            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(DTE), this.dte = new DTEMock());
            this.serviceProvider.RegisterService(typeof(SComponentModel), 
                ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<ITelemetryLogger>(this.logger = new ConfigurableTelemetryLogger())));

            this.solutionRootFolder = Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName);
            this.dte.Solution = new SolutionMock(dte, Path.Combine(this.solutionRootFolder, "solution.sln"));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            KnownUIContextsAccessor.Reset();
        }

        #region Tests
        [TestMethod]
        public void BoundSolutionAnalyzer_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new BoundSolutionAnalyzer(null));
        }

        [TestMethod]
        public void BoundSolutionAnalyzer_HasNoRuleSetsInSonarQubeDirectory()
        {
            // Setup
            string sonarQubeDirectory = Path.Combine(this.solutionRootFolder, BoundSolutionAnalyzer.SonarQubeFilesFolder);
            DeleteBindingInformationFile(sonarQubeDirectory);
            using (var testSubject = new BoundSolutionAnalyzer(this.serviceProvider))
            {

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Verify
                this.logger.AssertNoEventWasWritten();
            }
        }

        [TestMethod]
        public void BoundSolutionAnalyzer_HasRuleSetsInSonarQubeDirectory()
        {
            // Setup
            string sonarQubeDirectory = Path.Combine(this.solutionRootFolder, BoundSolutionAnalyzer.SonarQubeFilesFolder);
            GenerateBindingInformationFile(sonarQubeDirectory);
            BoundSolutionAnalyzer testSubject = null;

            try
            {
                // Case 1: Context is already active
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Act
                testSubject = new BoundSolutionAnalyzer(this.serviceProvider);

                // Verify
                this.logger.AssertSingleEventWasWritten(TelemetryEvent.BoundSolutionDetected);

                // Case 2: Context deactivated
                this.logger.Reset();

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, false);

                // Verify
                this.logger.AssertNoEventWasWritten();

                // Case 3: Context activated
                this.logger.Reset();

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Verify
                this.logger.AssertSingleEventWasWritten(TelemetryEvent.BoundSolutionDetected);

                // Case 4: reactivate when disposed
                this.logger.Reset();
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, false);
                testSubject.Dispose();
                testSubject = null;

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Verify
                this.logger.AssertNoEventWasWritten();
            }
            finally
            {
                testSubject?.Dispose();
                DeleteBindingInformationFile(sonarQubeDirectory);
            }
        }
        #endregion

        #region Helpers
        private static void GenerateBindingInformationFile(string directory)
        {
            if (Directory.Exists(directory))
            {
                DeleteBindingInformationFile(directory);
            }
            else
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(Path.Combine(directory, SolutionBindingSerializer.SonarQubeSolutionBindingConfigurationFileName), string.Empty);
        }

        private static void DeleteBindingInformationFile(string directory)
        {
            if (Directory.Exists(directory))
            {
                var filesToDelete = Directory.EnumerateFiles(directory, BoundSolutionAnalyzer.SonarQubeSolutionBindingConfigurationSearchPattern).ToList();
                filesToDelete.ForEach(File.Delete);
            }
        }
        #endregion  
    }
}
