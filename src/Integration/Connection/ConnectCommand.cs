//-----------------------------------------------------------------------
// <copyright file="ConnectCommand.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class ConnectCommand : HostedCommandBase, IConnectionInformationProvider, IConnectionWorkflowExecutor
    {
        private readonly IConnectionInformationProvider connectionProvider;
        private readonly ConnectSectionController controller;

        public ConnectCommand(ConnectSectionController controller, ISonarQubeServiceWrapper sonarQubeService)
            : this(controller, sonarQubeService, null, null)
        {
        }

        internal /*for testing purposes*/ ConnectCommand(ConnectSectionController controller, ISonarQubeServiceWrapper sonarQubeService, IConnectionInformationProvider connectionProvider, IConnectionWorkflowExecutor workflowExecutor)
            : base(controller)
        {
            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }

            this.controller = controller;
            this.SonarQubeService = sonarQubeService;
            this.WorkflowExecutor = workflowExecutor ?? this;
            this.connectionProvider = connectionProvider ?? this;
            this.WpfCommand = new RelayCommand(this.OnConnect, this.OnConnectStatus);
        }

        /// <summary>
        /// The connected server projects have changed
        /// </summary>
        /// <remarks>When the <see cref="ConnectedProjectsEventArgs.Projects"/> are null it means that the connection is disconnected.</remarks>
        public event EventHandler<ConnectedProjectsEventArgs> ProjectsChanged;

        #region Properties

        public RelayCommand WpfCommand
        {
            get;
        }


        internal IConnectionWorkflowExecutor WorkflowExecutor
        {
            get;
        }


        internal ISonarQubeServiceWrapper SonarQubeService
        {
            get;
        }

        internal ConnectionInformation LastAttemptedConnection { get; private set; }

        internal bool IsConnectionInProgress
        {
            get
            {
                return this.controller.IsConnecting;
            }
            set
            {
                if (this.controller.IsConnecting != value)
                {
                    this.controller.IsConnecting = value;
                    this.WpfCommand.RequeryCanExecute();
                }
            }
        }
        #endregion

        #region Command

        private bool OnConnectStatus()
        {
            return this.SonarQubeService.CurrentConnection == null
                && this.ProgressControlHost != null
                && !this.controller.IsConnecting;
        }

        private void OnConnect()
        {
            Debug.Assert(this.OnConnectStatus());
            Debug.Assert(!this.controller.IsConnecting, "Service is in a connecting state");

            var connectionInfo = this.connectionProvider.GetConnectionInformation(this.LastAttemptedConnection);
            if (connectionInfo != null)
            {
                this.EstablishConnection(connectionInfo);
            }
        }
        #endregion

        #region IConnectionInformationProvider

        ConnectionInformation IConnectionInformationProvider.GetConnectionInformation(ConnectionInformation currentConnection)
        {
            var dialog = new ConnectionInformationDialog();
            return dialog.ShowDialog(currentConnection);
        }

        #endregion

        #region IConnectionWorkflowExecutor

        public void EstablishConnection(ConnectionInformation connectionInfo)
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            this.LastAttemptedConnection = connectionInfo;

            this.WorkflowExecutor.EstablishConnection(connectionInfo, this.ConnectedProjectsChanged);
        }

        void IConnectionWorkflowExecutor.EstablishConnection(ConnectionInformation information, ConnectedProjectsCallback connectedProjectsChanged)
        {
            ConnectionWorkflow workflow = new ConnectionWorkflow(this, connectedProjectsChanged);
            IProgressEvents progressEvents = workflow.Run(information);
            this.SetConnectionInProgress(progressEvents);
        }

        internal /*for testing purposes*/ void SetConnectionInProgress(IProgressEvents progressEvents)
        {
            this.IsConnectionInProgress = true;

            ProgressNotificationListener progressListener = new ProgressNotificationListener(this.ServiceProvider, progressEvents);
            progressListener.MessageFormat = Strings.ConnectingToSonarQubePrefixMessageFormat;

            progressEvents.RunOnFinished(r =>
            {
                progressListener.Dispose();

                this.IsConnectionInProgress = false;
            });
        }

        internal /*for testing purposes*/ void ConnectedProjectsChanged(ConnectionInformation connection, IEnumerable<ProjectInformation> projects)
        {
            this.ProjectsChanged?.Invoke(this, new ConnectedProjectsEventArgs(connection, projects));
        }

        #endregion

    }
}
