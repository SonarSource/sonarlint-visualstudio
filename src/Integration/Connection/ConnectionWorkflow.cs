//-----------------------------------------------------------------------
// <copyright file="ConnectionWorkflow.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.Connection
{
    /// <summary>
    /// Workflow execution for the connect command.
    /// </summary>
    internal class ConnectionWorkflow
    {
        private readonly IHost host;
        private readonly ICommand parentCommand;
        private readonly IProjectSystemHelper projectSystem;

        public ConnectionWorkflow(IHost host, ICommand parentCommand)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (parentCommand == null)
            {
                throw new ArgumentNullException(nameof(parentCommand));
            }

            this.host = host;
            this.parentCommand = parentCommand;
            this.projectSystem = this.host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
        }

        internal /*for testing purposes*/ ConnectionInformation ConnectedServer
        {
            get;
            set;
        }

        internal bool IsCSharpPluginSupported { get; private set; }
        internal bool IsVBNetPluginSupported { get; private set; }

        #region Start the workflow
        public IProgressEvents Run(ConnectionInformation information)
        {
            Debug.Assert(this.host.ActiveSection != null, "Expect the section to be attached at least until this method returns");

            this.OnProjectsChanged(information, null);
            IProgressEvents progress = ProgressStepRunner.StartAsync(this.host, this.host.ActiveSection.ProgressHost, (controller) => this.CreateConnectionSteps(controller, information));
            this.DebugOnly_MonitorProgress(progress);
            return progress;
        }

        [Conditional("DEBUG")]
        private void DebugOnly_MonitorProgress(IProgressEvents progress)
        {
            progress.RunOnFinished(r => VsShellUtils.WriteToSonarLintOutputPane(this.host, "DEBUGONLY: Connect workflow finished, Execution result: {0}", r));
        }

        private ProgressStepDefinition[] CreateConnectionSteps(IProgressController controller, ConnectionInformation connection)
        {
            string connectStepDisplayText = string.Format(CultureInfo.CurrentCulture, Resources.Strings.ConnectingToSever, connection.ServerUri);
            return new[]
            {
                    new ProgressStepDefinition(connectStepDisplayText, StepAttributes.Indeterminate | StepAttributes.BackgroundThread,
                        (cancellationToken, notifications) => this.ConnectionStep(controller, cancellationToken, connection, notifications)),

                    new ProgressStepDefinition(connectStepDisplayText, StepAttributes.BackgroundThread,
                        (token, notifications) => this.DownloadServiceParameters(controller, token, notifications)),

                };
        }

        #endregion

        #region Workflow events

        private void OnProjectsChanged(ConnectionInformation connection, ProjectInformation[] projects)
        {
            this.host.VisualStateManager.SetProjects(connection, projects);
        }

        #endregion

        #region Workflow steps

        internal /* for testing purposes */ void ConnectionStep(IProgressController controller, CancellationToken cancellationToken, ConnectionInformation connection, IProgressStepExecutionEvents notifications)
        {
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToConnectId);
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.BadServerPluginId);

            notifications.ProgressChanged(connection.ServerUri.ToString());

            if (!this.VerifyDotNetPlugins(controller, cancellationToken, connection, notifications))
            {
                return;
            }

            this.ConnectedServer = connection;

            ProjectInformation[] projects;
            if (!this.host.SonarQubeService.TryGetProjects(connection, cancellationToken, out projects))
            {
                notifications.ProgressChanged(cancellationToken.IsCancellationRequested ? Strings.ConnectionResultCancellation : Strings.ConnectionResultFailure);
                this.host.ActiveSection?.UserNotifications?.ShowNotificationError(Strings.ConnectionFailed, NotificationIds.FailedToConnectId, this.parentCommand);

                AbortWorkflow(controller, cancellationToken);
                return;
            }

            this.OnProjectsChanged(connection, projects);
            notifications.ProgressChanged(Strings.ConnectionResultSuccess);
        }

        internal /*for testing purposes*/ void DownloadServiceParameters(IProgressController controller, CancellationToken token, IProgressStepExecutionEvents notifications)
        {
            Debug.Assert(this.ConnectedServer != null);

            // Should never realistically take more than 1 second to match against a project name
            var timeout = TimeSpan.FromSeconds(1);
            var defaultRegex = new Regex(ServerProperty.TestProjectRegexDefaultValue, RegexOptions.IgnoreCase, timeout);

            notifications.ProgressChanged(Strings.DownloadingServerSettingsProgessMessage);

            ServerProperty[] properties;
            if (!this.host.SonarQubeService.TryGetProperties(this.ConnectedServer, token, out properties) || token.IsCancellationRequested)
            {
                AbortWorkflow(controller, token);
                return;
            }

            var testProjRegexProperty = properties.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.Key, ServerProperty.TestProjectRegexKey));

            // Using this older API, if the property hasn't been explicitly set no default value is returned.
            // http://jira.sonarsource.com/browse/SONAR-5891
            var testProjRegexPattern = testProjRegexProperty?.Value ?? ServerProperty.TestProjectRegexDefaultValue;

            Regex regex = null;
            if (testProjRegexPattern != null)
            {
                // Try and create regex from provided server pattern.
                // No way to determine a valid pattern other than attempting to construct
                // the Regex object.
                try
                {
                    regex = new Regex(testProjRegexPattern, RegexOptions.IgnoreCase, timeout);
                }
                catch (ArgumentException)
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.InvalidTestProjectRegexPattern, testProjRegexPattern);
                }
            }

            var projectFilter = this.host.GetService<IProjectSystemFilter>();
            projectFilter.AssertLocalServiceIsNotNull();
            projectFilter.SetTestRegex(regex ?? defaultRegex);
        }

        #endregion

        #region Helpers

        private bool VerifyDotNetPlugins(IProgressController controller, CancellationToken cancellationToken, ConnectionInformation connection, IProgressStepExecutionEvents notifications)
        {
            notifications.ProgressChanged(Strings.DetectingServerPlugins);

            ServerPlugin[] plugins;
            if (!this.host.SonarQubeService.TryGetPlugins(connection, cancellationToken, out plugins))
            {
                notifications.ProgressChanged(cancellationToken.IsCancellationRequested ? Strings.ConnectionResultCancellation : Strings.ConnectionResultFailure);
                this.host.ActiveSection?.UserNotifications?.ShowNotificationError(Strings.ConnectionFailed, NotificationIds.FailedToConnectId, this.parentCommand);

                AbortWorkflow(controller, cancellationToken);
                return false;
            }

            IsCSharpPluginSupported = VerifyPluginSupport(plugins, MinimumSupportedServerPlugin.CSharp);
            IsVBNetPluginSupported = VerifyPluginSupport(plugins, MinimumSupportedServerPlugin.VbNet);

            var projects = this.projectSystem.GetSolutionProjects().ToList();
            var anyCSharpProject = projects.Any(p => MinimumSupportedServerPlugin.CSharp.ISupported(p));
            var anyVbNetProject = projects.Any(p => MinimumSupportedServerPlugin.VbNet.ISupported(p));

            string errorMessage;
            if ((IsCSharpPluginSupported && anyCSharpProject) ||
                (IsVBNetPluginSupported && anyVbNetProject))
            {
                return true;
            }
            else if (!IsCSharpPluginSupported && !IsVBNetPluginSupported)
            {
                errorMessage = Strings.ServerHasNoSupportedPluginVersion;
            }
            else if (projects.Count == 0)
            {
                errorMessage = Strings.SolutionContainsNoSupportedProject;
            }
            else if (IsCSharpPluginSupported && !anyCSharpProject)
            {
                errorMessage = string.Format(Strings.OnlySupportedPluginHasNoProjectInSolution, Language.CSharp.Name);
            }
            else
            {
                errorMessage = string.Format(Strings.OnlySupportedPluginHasNoProjectInSolution, Language.VBNET.Name);
            }

            this.host.ActiveSection?.UserNotifications?.ShowNotificationError(errorMessage, NotificationIds.BadServerPluginId, null);
            VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SubTextPaddingFormat, errorMessage);
            notifications.ProgressChanged(Strings.ConnectionResultFailure);

            AbortWorkflow(controller, cancellationToken);
            return false;
        }

        private bool VerifyPluginSupport(ServerPlugin[] plugins, MinimumSupportedServerPlugin minimumSupportedPlugin)
        {
            var plugin = plugins.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.Key, minimumSupportedPlugin.Key));
            var isPluginSupported = !string.IsNullOrWhiteSpace(plugin?.Version) && VersionHelper.Compare(plugin.Version, minimumSupportedPlugin.MinimumVersion) >= 0;

            var pluginSupportMessageFormat = string.Format(Strings.SubTextPaddingFormat, isPluginSupported ? Strings.SupportedPluginFoundMessage : Strings.UnsupportedPluginFoundMessage);
            VsShellUtils.WriteToSonarLintOutputPane(this.host, pluginSupportMessageFormat, minimumSupportedPlugin.ToString());

            return isPluginSupported;
        }

        private static void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");
        }

        #endregion
    }
}
