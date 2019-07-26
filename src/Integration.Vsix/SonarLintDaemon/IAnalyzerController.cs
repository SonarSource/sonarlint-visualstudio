/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal interface IAnalyzerController
    {
        bool IsAnalysisSupported(IEnumerable<SonarLanguage> languages);

        void RequestAnalysis(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem);
    }

    [Export(typeof(IAnalyzerController))]
    internal class AnalyzerController : IAnalyzerController
    {
        private readonly ISonarLintSettings settings;
        private readonly ISonarLintDaemon daemon;
        private readonly ILogger logger;

        [ImportingConstructor]
        public AnalyzerController(
            ISonarLintSettings settings,
            ISonarLintDaemon daemon,
            ILogger logger
            )
        {
            this.settings = settings;
            this.daemon = daemon;
            this.logger = logger;
        }

        public bool IsAnalysisSupported(IEnumerable<SonarLanguage> languages)
        {
            if (languages.Contains(SonarLanguage.CFamily))
            {
                return true;
            }
            
            // TODO: this method is called when deciding whether to create a tagger.
            // If support for additional languages is not active when the user opens a document
            // then we won't create a tagger. If the user then activates support for additional
            // languages and saves the file, we won't display any issues because there isn't a
            // tagger. The user will have to close and re-open the file to see issues.
            // Is this the behaviour we want, or should we return true here if the language
            // is supported, and then check whether to analyze when RequestAnalysis is called?
            bool isSupported = (languages.Contains(SonarLanguage.Javascript) &&
                settings.IsActivateMoreEnabled);
            return isSupported;
        }

        public void RequestAnalysis(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
        {
            bool handled = false;
            foreach (var language in detectedLanguages)
            {
                switch (language)
                {
                    case SonarLanguage.Javascript:
                        handled = true;
                        if (settings.IsActivateMoreEnabled)
                        {
                            // User might have disable additional languages in the meantime
                            break;
                        }

                        if (!daemon.IsRunning) // daemon might not have finished starting / might have shutdown
                        {
                            logger.WriteLine("Daemon has not started yet. Analysis will not be performed");
                            break;
                        }

                        daemon.RequestAnalysis(path, charset, "js", consumer);
                        break;

                    case SonarLanguage.CFamily:
                        handled = true;
                        CFamilyHelper.ProcessFile(new ProcessRunner(logger), consumer, logger, projectItem, path, charset);
                        break;

                    default:
                        break;
                }
            }

            if (!handled)
            {
                logger.WriteLine($"Unsupported content type for {path}");
            }
        }

    }

}
