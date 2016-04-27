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
        }

        internal /*for testing purposes*/ ConnectionInformation ConnectedServer
        {
            get;
            set;
        }

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

            notifications.ProgressChanged(connection.ServerUri.ToString(), double.NaN);

            if (!this.VerifyCSharpPlugin(controller, cancellationToken, connection, notifications))
            {
                return;
            }

            this.ConnectedServer = connection;

            ProjectInformation[] projects;
            if (!this.host.SonarQubeService.TryGetProjects(connection, cancellationToken, out projects))
            {
                notifications.ProgressChanged(cancellationToken.IsCancellationRequested ? Strings.ConnectionResultCancellation : Strings.ConnectionResultFailure, double.NaN);
                this.host.ActiveSection?.UserNotifications?.ShowNotificationError(Strings.ConnectionFailed, NotificationIds.FailedToConnectId, this.parentCommand);

                AbortWorkflow(controller, cancellationToken);
                return;
            }

            this.OnProjectsChanged(connection, projects);
            notifications.ProgressChanged(Strings.ConnectionResultSuccess, double.NaN);
        }


        internal /*for testing purposes*/ void DownloadServiceParameters(IProgressController controller, CancellationToken token, IProgressStepExecutionEvents notifications)
        {
            Debug.Assert(this.ConnectedServer != null);

            // Should never realistically take more than 1 second to match against a project name
            var timeout = TimeSpan.FromSeconds(1);
            var defaultRegex = new Regex(ServerProperty.TestProjectRegexDefaultValue, RegexOptions.IgnoreCase, timeout);

            notifications.ProgressChanged(Strings.PreparingBindingWorkflowProgessMessage, double.NaN);

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

        private bool VerifyCSharpPlugin(IProgressController controller, CancellationToken cancellationToken, ConnectionInformation connection, IProgressStepExecutionEvents notifications)
        {
            ServerPlugin[] plugins;
            if (!this.host.SonarQubeService.TryGetPlugins(connection, cancellationToken, out plugins))
            {
                notifications.ProgressChanged(cancellationToken.IsCancellationRequested ? Strings.ConnectionResultCancellation : Strings.ConnectionResultFailure, double.NaN);
                this.host.ActiveSection?.UserNotifications?.ShowNotificationError(Strings.ConnectionFailed, NotificationIds.FailedToConnectId, this.parentCommand);

                AbortWorkflow(controller, cancellationToken);
                return false;
            }

            var csPlugin = plugins.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.Key, ServerPlugin.CSharpPluginKey));
            if (string.IsNullOrWhiteSpace(csPlugin?.Version) || VersionHelper.Compare(csPlugin.Version, ServerPlugin.CSharpPluginMinimumVersion) < 0)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, Strings.ServerDoesNotHaveCorrectVersionOfCSharpPlugin, ServerPlugin.CSharpPluginMinimumVersion);

                this.host.ActiveSection?.UserNotifications?.ShowNotificationError(errorMessage, NotificationIds.BadServerPluginId, null);
                notifications.ProgressChanged(errorMessage, double.NaN);
                notifications.ProgressChanged(Strings.ConnectionResultFailure, double.NaN);

                AbortWorkflow(controller, cancellationToken);
                return false;
            }

            return true;
        }

        private static void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");
        }

        #endregion
    }
}
