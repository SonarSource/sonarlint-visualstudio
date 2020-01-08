/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Vsix;

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
        public void BoundSolutionAnalyzer_HasNoRuleSetsInSonarQubeDirectory_Legacy()
        {
            // Arrange
            string sonarQubeDirectory = Path.Combine(this.solutionRootFolder, Constants.LegacySonarQubeManagedFolderName);
            DeleteBindingInformationFile(sonarQubeDirectory);
            using (var testSubject = new BoundSolutionAnalyzer(this.serviceProvider))
            {
                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Assert
                this.logger.AssertNoEventWasWritten();
            }
        }

        [TestMethod]
        public void BoundSolutionAnalyzer_HasNoRuleSetsInSonarQubeDirectory_Connected()
        {
            // Arrange
            string sonarQubeDirectory = Path.Combine(this.solutionRootFolder, Constants.SonarlintManagedFolderName);
            DeleteBindingInformationFile(sonarQubeDirectory);
            using (var testSubject = new BoundSolutionAnalyzer(this.serviceProvider))
            {
                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Assert
                this.logger.AssertNoEventWasWritten();
            }
        }

        [TestMethod]
        public void BoundSolutionAnalyzer_HasRuleSetsInSonarQubeDirectory_Legacy()
        {
            // Arrange
            string sonarQubeDirectory = Path.Combine(this.solutionRootFolder, Constants.LegacySonarQubeManagedFolderName);
            GenerateBindingInformationFile(sonarQubeDirectory, SolutionBindingSerializer.LegacyBindingConfigurationFileName);
            BoundSolutionAnalyzer testSubject = null;

            try
            {
                // Case 1: Context is already active
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Act
                testSubject = new BoundSolutionAnalyzer(this.serviceProvider);

                // Assert
                this.logger.AssertSingleEventWasWritten(TelemetryEvent.BoundSolutionDetected);

                // Case 2: Context deactivated
                this.logger.Reset();

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, false);

                // Assert
                this.logger.AssertNoEventWasWritten();

                // Case 3: Context activated
                this.logger.Reset();

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Assert
                this.logger.AssertSingleEventWasWritten(TelemetryEvent.BoundSolutionDetected);

                // Case 4: reactivate when disposed
                this.logger.Reset();
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, false);
                testSubject.Dispose();
                testSubject = null;

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Assert
                this.logger.AssertNoEventWasWritten();
            }
            finally
            {
                testSubject?.Dispose();
                DeleteBindingInformationFile(sonarQubeDirectory);
            }
        }

        [TestMethod]
        public void BoundSolutionAnalyzer_HasRuleSetsInSonarQubeDirectory_Connected()
        {
            // Arrange
            string sonarQubeDirectory = Path.Combine(this.solutionRootFolder, Constants.SonarlintManagedFolderName);
            GenerateBindingInformationFile(sonarQubeDirectory, "foo.slconfig");
            BoundSolutionAnalyzer testSubject = null;

            try
            {
                // Case 1: Context is already active
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Act
                testSubject = new BoundSolutionAnalyzer(this.serviceProvider);

                // Assert
                this.logger.AssertSingleEventWasWritten(TelemetryEvent.BoundSolutionDetected);

                // Case 2: Context deactivated
                this.logger.Reset();

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, false);

                // Assert
                this.logger.AssertNoEventWasWritten();

                // Case 3: Context activated
                this.logger.Reset();

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Assert
                this.logger.AssertSingleEventWasWritten(TelemetryEvent.BoundSolutionDetected);

                // Case 4: reactivate when disposed
                this.logger.Reset();
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, false);
                testSubject.Dispose();
                testSubject = null;

                // Act
                this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);

                // Assert
                this.logger.AssertNoEventWasWritten();
            }
            finally
            {
                testSubject?.Dispose();
                DeleteBindingInformationFile(sonarQubeDirectory);
            }
        }

        #endregion Tests

        #region Helpers

        private static void GenerateBindingInformationFile(string directory, string fileName)
        {
            if (Directory.Exists(directory))
            {
                DeleteBindingInformationFile(directory);
            }
            else
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(Path.Combine(directory, fileName), string.Empty);
        }

        private static void DeleteBindingInformationFile(string directory)
        {
            if (Directory.Exists(directory))
            {
                var filesToDelete = Directory.EnumerateFiles(directory, BoundSolutionAnalyzer.BindingConfigurationSearchPattern).ToList();
                filesToDelete.ForEach(File.Delete);
            }
        }

        #endregion Helpers
    }
}
