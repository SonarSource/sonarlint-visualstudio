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
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CFamilyReproducerCommand
    {
        // Command set guid and command id. Must match those in DaemonCommands.vsct
        public static readonly Guid CommandSet = new Guid("1F83EA11-3B07-45B3-BF39-307FD4F42194");
        public const int CommandId = 0x0300;

        private readonly OleMenuCommand menuItem;
        private readonly IActiveDocumentLocator activeDocumentLocator;
        private readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        private readonly IAnalysisRequester analysisRequester;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package, ILogger logger)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var monitorSelection = await package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var adapterService = await package.GetMefServiceAsync<IVsEditorAdaptersFactoryService>();
            var docLocator = new ActiveDocumentLocator(monitorSelection, adapterService);

            var languageRecognizer = await package.GetMefServiceAsync<ISonarLanguageRecognizer>();
            var requester = await package.GetMefServiceAsync<IAnalysisRequester>();

            IMenuCommandService commandService = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
            Instance = new CFamilyReproducerCommand(commandService, docLocator, languageRecognizer, requester, logger);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableRuleCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="menuCommandService">Command service to add command to, not null.</param>
        internal /* for testing */ CFamilyReproducerCommand(IMenuCommandService menuCommandService,
            IActiveDocumentLocator activeDocumentLocator, ISonarLanguageRecognizer languageRecognizer,
            IAnalysisRequester analysisRequester, ILogger logger)
        {
            if (menuCommandService == null)
            {
                throw new ArgumentNullException(nameof(menuCommandService));
            }

            this.activeDocumentLocator = activeDocumentLocator ?? throw new ArgumentNullException(nameof(activeDocumentLocator));
            this.sonarLanguageRecognizer = languageRecognizer ?? throw new ArgumentNullException(nameof(languageRecognizer));
            this.analysisRequester = analysisRequester ?? throw new ArgumentNullException(nameof(analysisRequester));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            menuItem = new OleMenuCommand(Execute, null, QueryStatus, menuCommandID);
            menuCommandService.AddCommand(menuItem);
        }

        private void QueryStatus(object sender, EventArgs args)
        {
            // Settings enabled to false will stop the command being executed.
            // VS will print "The command is not available" in the Command window.
            try
            {
                menuItem.Visible = false;
                menuItem.Enabled = HasActiveCFamilyDoc();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CFamilyStrings.ReproCmd_Error_QueryStatus, ex.Message);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CFamilyReproducerCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            try
            {
                TriggerReproducer();
            }
            catch(Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CFamilyStrings.ReproCmd_Error_Execute, ex.Message);
            }
        }

        private bool HasActiveCFamilyDoc()
        {
            var activeDoc = activeDocumentLocator.FindActiveDocument();
            if (activeDoc == null)
            {
                logger.WriteLine(CFamilyStrings.ReproCmd_NoActiveDocument);
                return false;
            }

            var languages = sonarLanguageRecognizer.Detect(activeDoc.FilePath, activeDoc.TextBuffer.ContentType);
            if (languages.Contains(AnalysisLanguage.CFamily))
            {
                logger.WriteLine(CFamilyStrings.ReproCmd_DocumentIsAnalyzable, activeDoc.FilePath);
                return true;
            }

            logger.WriteLine(CFamilyStrings.ReproCmd_DocumentIsNotAnalyzable, activeDoc.FilePath);
            return false;
        }

        private void TriggerReproducer()
        {
            Debug.Assert(HasActiveCFamilyDoc(), "Expecting a document that can be analyzed by the CFamily analyzer to be active");

            var activeDoc = activeDocumentLocator.FindActiveDocument();

            if (activeDoc != null)
            {
                var options = new CFamilyAnalyzerOptions
                {
                    CreateReproducer = true
                };

                logger.WriteLine(CFamilyStrings.ReproCmd_ExecutingReproducer);
                analysisRequester.RequestAnalysis(options, activeDoc.FilePath);
            }
        }
    }
}
