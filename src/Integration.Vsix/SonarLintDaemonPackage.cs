/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.CSharpVB;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Events;
using SonarLint.VisualStudio.Integration.Vsix.Events.Build;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.RoslynAnalyzerServer;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Analysis;
using ErrorHandler = SonarLint.VisualStudio.Core.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ExcludeFromCodeCoverage]
    public sealed class SonarLintDaemonPackage : AsyncPackage
    {
        public const string PackageGuidString = "6f63ab5a-5ab8-4a0d-9914-151911885966";

        public const string CommandSetGuidString = "1F83EA11-3B07-45B3-BF39-307FD4F42194";

        private ILogger logger;
        private IActiveCompilationDatabaseTracker activeCompilationDatabaseTracker;
        private IProjectDocumentsEventsListener projectDocumentsEventsListener;
        private ISLCoreHandler slCoreHandler;
        private IDocumentEventsHandler documentEventsHandler;
        private ISlCoreUserAnalysisPropertiesSynchronizer slCoreUserAnalysisPropertiesSynchronizer;
        private IAnalysisConfigMonitor analysisConfigMonitor;
        private IBuildEventNotifier buildEventNotifier;
        private IRoslynEnvironmentInitializer roslynEnvironment;
        private IFailedPluginNotification failedPluginNotification;

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarLintDaemonPackage"/> class.
        /// </summary>
        public SonarLintDaemonPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.RunAsync(InitAsync);
        }

        private async Task InitAsync()
        {
            try
            {
                logger = await this.GetMefServiceAsync<ILogger>();
                logger.WriteLine(Strings.Daemon_Initializing);
                logger.WriteLine(Strings.SQVSVersionLog, VersionHelper.SonarLintVersion);

                logger.WriteLine("[DEBUG] Resolving IImportBeforeFileGenerator...");
                var importBeforeFileGenerator = await this.GetMefServiceAsync<IImportBeforeFileGenerator>();
                logger.WriteLine("[DEBUG] Initializing IImportBeforeFileGenerator (resolved: {0})...", importBeforeFileGenerator != null);
                await importBeforeFileGenerator.InitializationProcessor.InitializeAsync();

                // This migration should be performed before initializing other services, independent if a solution or a folder is opened.
                logger.WriteLine("[DEBUG] Migrating bindings to server connections...");
                await MigrateBindingsToServerConnectionsIfNeededAsync();
                logger.WriteLine("[DEBUG] Bindings migration complete.");

                logger.WriteLine("[DEBUG] Initializing commands...");
                await MuteIssueCommand.InitializeAsync(this, logger);
                await DisableRuleCommand.InitializeAsync(this, logger);

                logger.WriteLine("[DEBUG] Resolving IActiveCompilationDatabaseTracker...");
                activeCompilationDatabaseTracker = await this.GetMefServiceAsync<IActiveCompilationDatabaseTracker>();
                logger.WriteLine("[DEBUG] Initializing IActiveCompilationDatabaseTracker (resolved: {0})...", activeCompilationDatabaseTracker != null);
                await activeCompilationDatabaseTracker.InitializationProcessor.InitializeAsync();

                logger.WriteLine("[DEBUG] Resolving ISlCoreUserAnalysisPropertiesSynchronizer...");
                slCoreUserAnalysisPropertiesSynchronizer = await this.GetMefServiceAsync<ISlCoreUserAnalysisPropertiesSynchronizer>();
                logger.WriteLine("[DEBUG] Initializing ISlCoreUserAnalysisPropertiesSynchronizer (resolved: {0})...", slCoreUserAnalysisPropertiesSynchronizer != null);
                await slCoreUserAnalysisPropertiesSynchronizer.InitializationProcessor.InitializeAsync();

                logger.WriteLine("[DEBUG] Resolving IAnalysisConfigMonitor...");
                analysisConfigMonitor = await this.GetMefServiceAsync<IAnalysisConfigMonitor>();
                logger.WriteLine("[DEBUG] Initializing IAnalysisConfigMonitor (resolved: {0})...", analysisConfigMonitor != null);
                await analysisConfigMonitor.InitializationProcessor.InitializeAsync();

                logger.WriteLine("[DEBUG] Resolving IDocumentEventsHandler...");
                documentEventsHandler = await this.GetMefServiceAsync<IDocumentEventsHandler>();
                logger.WriteLine("[DEBUG] IDocumentEventsHandler resolved: {0}.", documentEventsHandler != null);

                logger.WriteLine("[DEBUG] Resolving IProjectDocumentsEventsListener...");
                projectDocumentsEventsListener = await this.GetMefServiceAsync<IProjectDocumentsEventsListener>();
                logger.WriteLine("[DEBUG] Initializing IProjectDocumentsEventsListener (resolved: {0})...", projectDocumentsEventsListener != null);
                projectDocumentsEventsListener.Initialize();

                logger.WriteLine("[DEBUG] Resolving IRoslynEnvironmentInitializer...");
                roslynEnvironment = await this.GetMefServiceAsync<IRoslynEnvironmentInitializer>();
                logger.WriteLine("[DEBUG] Initializing IRoslynEnvironmentInitializer (resolved: {0})...", roslynEnvironment != null);
                await roslynEnvironment.InitializationProcessor.InitializeAsync();

                logger.WriteLine("[DEBUG] Resolving IBuildEventNotifier...");
                buildEventNotifier = await this.GetMefServiceAsync<IBuildEventNotifier>();
                logger.WriteLine("[DEBUG] Initializing IBuildEventNotifier (resolved: {0})...", buildEventNotifier != null);
                await buildEventNotifier.InitializationProcessor.InitializeAsync();

                logger.WriteLine("[DEBUG] Resolving IFailedPluginNotification...");
                failedPluginNotification = await this.GetMefServiceAsync<IFailedPluginNotification>();
                logger.WriteLine("[DEBUG] IFailedPluginNotification resolved: {0}.", failedPluginNotification != null);

                logger.WriteLine("[DEBUG] Resolving ISLCoreHandler...");
                slCoreHandler = await this.GetMefServiceAsync<ISLCoreHandler>();
                logger.WriteLine("[DEBUG] Enabling sloop (ISLCoreHandler resolved: {0})...", slCoreHandler != null);
                slCoreHandler.EnableSloop();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger?.WriteLine(Strings.ERROR_InitializingDaemon, ex);
            }
            logger?.WriteLine(Strings.Daemon_InitializationComplete);
        }

        private async Task MigrateBindingsToServerConnectionsIfNeededAsync()
        {
            logger.WriteLine("[DEBUG] Resolving IBindingToConnectionMigration...");
            var bindingToConnectionMigration = await this.GetMefServiceAsync<IBindingToConnectionMigration>();
            logger.WriteLine("[DEBUG] Running migration (IBindingToConnectionMigration resolved: {0})...", bindingToConnectionMigration != null);
            await bindingToConnectionMigration.MigrateAllBindingsToServerConnectionsIfNeededAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                analysisConfigMonitor?.Dispose();
                analysisConfigMonitor = null;

                slCoreUserAnalysisPropertiesSynchronizer?.Dispose();
                slCoreUserAnalysisPropertiesSynchronizer = null;
                activeCompilationDatabaseTracker?.Dispose();
                activeCompilationDatabaseTracker = null;

                documentEventsHandler?.Dispose();
                documentEventsHandler = null;

                projectDocumentsEventsListener?.Dispose();
                projectDocumentsEventsListener = null;
                slCoreHandler?.Dispose();
                slCoreHandler = null;

                roslynEnvironment?.Dispose();
                roslynEnvironment = null;

                buildEventNotifier?.Dispose();
                buildEventNotifier = null;

                failedPluginNotification?.Dispose();
                failedPluginNotification = null;
            }
        }

        #endregion
    }
}
