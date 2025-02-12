/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Transition;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Transition
{
    [Export(typeof(IMuteIssuesService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class MuteIssuesService : IMuteIssuesService
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ILogger logger;
        private readonly IMuteIssuesWindowService muteIssuesWindowService;
        private readonly IThreadHandling threadHandling;
        private readonly IMessageBox messageBox;
        private readonly ResourceManager resourceManager;

        [ImportingConstructor]
        public MuteIssuesService(IActiveSolutionBoundTracker activeSolutionBoundTracker, ILogger logger, IMuteIssuesWindowService muteIssuesWindowService)
            : this(activeSolutionBoundTracker, logger, muteIssuesWindowService, ThreadHandling.Instance, new Core.MessageBox())
        { }

        internal MuteIssuesService(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ILogger logger,
            IMuteIssuesWindowService muteIssuesWindowService,
            IThreadHandling threadHandling,
            IMessageBox messageBox)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.logger = logger;
            this.muteIssuesWindowService = muteIssuesWindowService;
            this.threadHandling = threadHandling;
            this.messageBox = messageBox;

            resourceManager = new ResourceManager(typeof(Resources));
        }

        public async Task ResolveIssueWithDialogAsync(string issueServerKey)
        {
            threadHandling.ThrowIfOnUIThread();

            if (!activeSolutionBoundTracker.CurrentConfiguration.Mode.IsInAConnectedMode())
            {
                logger.LogVerbose(Resources.MuteWindowService_NotInConnectedMode);
                return;
            }

            MuteIssuesWindowResponse windowResponse = default;

            await threadHandling.RunOnUIThreadAsync(() => windowResponse = muteIssuesWindowService.Show());

            if (windowResponse.Result)
            {
                // Success
                // if ()
                // {
                //     issue.IsResolved = true;
                // }
                //
                // Fail
                // if ()
                // {
                //     // ideally, message box invocation should be moved to Mute command
                //     messageBox.Show(resourceManager.GetString($"MuteIssuesService_Error_{serviceResult}"), Resources.MuteIssuesService_Error_Caption, MessageBoxButton.OK, MessageBoxImage.Error);
                // }
            }
        }
    }
}
