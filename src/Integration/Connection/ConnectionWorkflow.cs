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
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.Connection
{
    /// <summary>
    /// Workflow execution for the connect command.
    /// </summary>
    internal class ConnectionWorkflow
    {
        private readonly ConnectCommand owner;
        private readonly ConnectedProjectsCallback connectedProjectsChanged;
        private readonly IIntegrationSettings settings;
        // TODO: this is the wrong place to have the ICommand, it should be higher up. Requires some refactoring.
        private readonly ICommand dontWarnAgainCommand;

        public ConnectionWorkflow(ConnectCommand owner, ConnectedProjectsCallback connectedProjectsChanged)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (connectedProjectsChanged == null)
            {
                throw new ArgumentNullException(nameof(connectedProjectsChanged));
            }

            this.owner = owner;
            this.connectedProjectsChanged = connectedProjectsChanged;
            this.settings = this.owner.ServiceProvider.GetMefService<IIntegrationSettings>();
            this.dontWarnAgainCommand = new RelayCommand(this.DontWarnAgainExec, this.DontWarnAgainCanExec);
        }

        #region Start the workflow
        public IProgressEvents Run(ConnectionInformation information)
        {
            Debug.Assert(!ReferenceEquals(information, this.owner.SonarQubeService.CurrentConnection), "Using the same connection instance - it will be disposed during the execution, so clone it before calling this method");

            this.OnProjectsChanged(information, null);
            IProgressEvents progress = ProgressStepRunner.StartAsync(this.owner.ServiceProvider, this.owner.ProgressControlHost, (controller) => this.CreateConnectionSteps(controller, information));
            this.DebugOnly_MonitorProgress(progress);
            return progress;
        }

        [Conditional("DEBUG")]
        private void DebugOnly_MonitorProgress(IProgressEvents progress)
        {
            progress.RunOnFinished(r => VsShellUtils.WriteToGeneralOutputPane(this.owner.ServiceProvider, "DEBUGONLY: Connect workflow finished, Execution result: {0}", r));
        }

        private ProgressStepDefinition[] CreateConnectionSteps(IProgressController controller, ConnectionInformation connection)
        {
            string connectStepDisplayText = string.Format(CultureInfo.CurrentCulture, Resources.Strings.ConnectingToSever, connection.ServerUri);
            return new[]
            {
                    new ProgressStepDefinition(connectStepDisplayText, StepAttributes.Indeterminate | StepAttributes.BackgroundThread, (cancellationToken, notifications) =>
                    {
                        this.ConnectionStep(controller, cancellationToken, connection, notifications);
                    }),
                    new ProgressStepDefinition(null, StepAttributes.NoProgressImpact | StepAttributes.Hidden | StepAttributes.NonCancellable, (token, events) =>
                    {
                        this.ShowNuGetWarning();
                    })
                };
        }

        #endregion

        #region Workflow events

        private void OnProjectsChanged(ConnectionInformation connection, ProjectInformation[] projects)
        {
            this.connectedProjectsChanged.Invoke(connection, projects);
        }

        #endregion

        #region Workflow steps

        private static void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");
        }

        internal /* for testing purposes */ void ConnectionStep(IProgressController controller, CancellationToken cancellationToken, ConnectionInformation connection, IProgressStepExecutionEvents notifications)
        {
            this.owner.UserNotification?.HideNotification(NotificationIds.FailedToConnectId);
            this.owner.UserNotification?.HideNotification(NotificationIds.BadServerPluginId);

            notifications.ProgressChanged(connection.ServerUri.ToString(), double.NaN);

            var plugins = this.owner.SonarQubeService.GetPlugins(connection, cancellationToken);

            if (plugins == null)
            {
                notifications.ProgressChanged(cancellationToken.IsCancellationRequested ? Strings.ConnectionResultCancellation : Strings.ConnectionResultFailure, double.NaN);
                this.owner.UserNotification?.ShowNotificationError(Strings.ConnectionFailed, NotificationIds.FailedToConnectId, this.owner.WpfCommand);

                AbortWorkflow(controller, cancellationToken);
                return;
            }

            var csPlugin = plugins.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.Key, ServerPlugin.CSharpPluginKey));
            if (string.IsNullOrWhiteSpace(csPlugin?.Version) || VersionHelper.Compare(csPlugin.Version, ServerPlugin.CSharpPluginMinimumVersion) < 0)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, Strings.ServerDoesNotHaveCorrectVersionOfCSharpPlugin, ServerPlugin.CSharpPluginMinimumVersion);

                this.owner.UserNotification?.ShowNotificationError(errorMessage, NotificationIds.BadServerPluginId, null);
                notifications.ProgressChanged(errorMessage, double.NaN);
                notifications.ProgressChanged(Strings.ConnectionResultFailure, double.NaN);

                AbortWorkflow(controller, cancellationToken);
                return;
            }

            ProjectInformation[] projects = this.owner.SonarQubeService.Connect(connection, cancellationToken)?.ToArray();

            if (this.owner.SonarQubeService.CurrentConnection == null)
            {
                notifications.ProgressChanged(cancellationToken.IsCancellationRequested ? Strings.ConnectionResultCancellation : Strings.ConnectionResultFailure, double.NaN);
                this.owner.UserNotification?.ShowNotificationError(Strings.ConnectionFailed, NotificationIds.FailedToConnectId, this.owner.WpfCommand);

                AbortWorkflow(controller, cancellationToken);
                return;
            }

            this.OnProjectsChanged(connection, projects);
            notifications.ProgressChanged(Strings.ConnectionResultSuccess, double.NaN);
        }

        internal /* for testing purposes */ void ShowNuGetWarning()
        {
            if (this.settings.ShowServerNuGetTrustWarning)
            {
                this.owner.UserNotification.ShowNotificationWarning(Strings.ServerNuGetTrustWarningMessage, NotificationIds.WarnServerTrustId, this.dontWarnAgainCommand);
            }
        }

        internal /* for testing purposes */ void DontWarnAgainExec()
        {
            this.settings.ShowServerNuGetTrustWarning = false;
            this.owner.UserNotification.HideNotification(NotificationIds.WarnServerTrustId);
        }

        internal /* for testing purposes */ bool DontWarnAgainCanExec()
        {
            return this.settings != null;
        }

        #endregion
    }
}
