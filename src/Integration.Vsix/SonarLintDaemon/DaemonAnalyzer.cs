/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IAnalyzer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DaemonAnalyzer : IAnalyzer
    {
        private readonly ISonarLintDaemon daemon;
        private readonly IDaemonInstaller installer;
        private readonly ITelemetryManager telemetryManager;

        [ImportingConstructor]
        public DaemonAnalyzer(ISonarLintDaemon daemon, IDaemonInstaller daemonInstaller, ITelemetryManager telemetryManager)
        {
            this.daemon = daemon;
            this.installer = daemonInstaller;
            this.telemetryManager = telemetryManager;
        }

        public bool IsAnalysisSupported(IEnumerable<SonarLanguage> languages)
        {
            return daemon.IsAnalysisSupported(languages);
        }

        public void RequestAnalysis(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
        {
            if (!IsAnalysisSupported(detectedLanguages))
            {
                return;
            }

            // Optimise for the common case of daemon up and running
            if (installer.IsInstalled() && daemon.IsRunning)
            {
                InvokeDaemon(path, charset, detectedLanguages, consumer, projectItem);
                return;
            }

            new DelayedRequest(this, path, charset, detectedLanguages, consumer, projectItem).Execute();
        }

        private void InvokeDaemon(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem)
        {
            Debug.Assert(detectedLanguages?.Contains(SonarLanguage.Javascript) ?? false, "Not expecting the daemon to be called for languages other than JavaScript");

            // TODO refactor the daemon so it does not implement IAnalyzer or make any
            // decisions about whether to run or not. That should all be handled by 
            // this class.
            telemetryManager.LanguageAnalyzed("js");
            daemon.RequestAnalysis(path, charset, detectedLanguages, consumer, projectItem);
        }

        /// <summary>
        /// Helper class that handle waiting until the daemon is installed and started before
        /// making the request.
        /// </summary>
        /// <remarks>We want to make sure the event handlers are unregistered correctly. We could have done
        /// this in the RequestAnalysis method above using event handler variables and lambdas and letting the
        /// compiler handle creating the closure to capture the variables.
        /// However, this version is easier to read.
        /// </remarks>
        private class DelayedRequest
        {
            private readonly ISonarLintDaemon daemon;
            private readonly IDaemonInstaller daemonInstaller;
            private readonly DaemonAnalyzer daemonAnalyzer;
            private readonly string path;
            private readonly string charset;
            private readonly IEnumerable<SonarLanguage> detectedLanguages;
            private readonly IIssueConsumer consumer;
            private readonly ProjectItem projectItem;

            public DelayedRequest(DaemonAnalyzer daemonAnalyzer, string path, string charset, IEnumerable<SonarLanguage> detectedLanguages,
                IIssueConsumer consumer, ProjectItem projectItem)
            {
                this.daemonAnalyzer = daemonAnalyzer;
                this.daemon = daemonAnalyzer.daemon;
                this.daemonInstaller = daemonAnalyzer.installer;
                this.path = path;
                this.charset = charset;
                this.detectedLanguages = detectedLanguages;
                this.consumer = consumer;
                this.projectItem = projectItem;
            }

            public void Execute()
            {
                if (!daemonInstaller.IsInstalled())
                {
                    daemonInstaller.InstallationCompleted += HandleInstallCompleted;
                    daemonInstaller.Install();
                }
                else
                {
                    if (!daemon.IsRunning)
                    {
                        daemon.Ready += HandleDaemonReady;
                        daemon.Start();
                    }
                    else
                    {
                        MakeRequest();
                    }
                }
            }

            private void MakeRequest()
            {
                daemon.Ready -= HandleDaemonReady;
                daemonInstaller.InstallationCompleted -= HandleInstallCompleted;
                daemonAnalyzer.InvokeDaemon(path, charset, detectedLanguages, consumer, projectItem);
            }

            private void HandleInstallCompleted(object sender, AsyncCompletedEventArgs e)
            {
                daemonInstaller.InstallationCompleted -= HandleInstallCompleted;

                if (e.Error == null && !e.Cancelled)
                {
                    daemon.Ready += HandleDaemonReady;
                    daemon.Start();
                }
            }

            private void HandleDaemonReady(object sender, EventArgs e)
            {
                MakeRequest();
            }
        }
    }
}
