﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal interface IAutoBindTrigger
    {
        void TriggerAfterSuccessfulWorkflow(IProgressEvents workflowProgress, bool isAutoBindRequested,
            string projectKey, ConnectionInformation connectionInformation);
    }
    
    [Export(typeof(IAutoBindTrigger))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AutoBindTrigger : IAutoBindTrigger
    {
        private readonly IHost host;

        [ImportingConstructor]
        public AutoBindTrigger(IHost host)
        {
            this.host = host;
        }
        
        public void TriggerAfterSuccessfulWorkflow(IProgressEvents workflowProgress, bool isAutoBindRequested,
            string projectKey, ConnectionInformation connectionInformation)
        {
            workflowProgress.RunOnFinished(result =>
            {
                AutobindIfPossible(result, isAutoBindRequested, projectKey, connectionInformation);
            });
        }

        internal /* for testing */ void AutobindIfPossible(ProgressControllerResult result, 
            bool isAutoBindRequested,
            string projectKey,
            ConnectionInformation connectionInformation)
        {
            if (result == ProgressControllerResult.Succeeded
                && isAutoBindRequested
                && !string.IsNullOrEmpty(projectKey))
            {
                host.ActiveSection.BindCommand.Execute(
                    new BindCommandArgs(projectKey, string.Empty, connectionInformation));
            }
        }
    }
}
