/*
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

using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.ConnectedMode_prototype
{
    // TE: TODO - there is an existing generic INotificationService service which uses the IInfoBarManager
    // internally. However, it's currently not generic enough and would need some refactoring to be used
    // here:
    // * it's hard-coded to the put the gold bar on the main window.
    // * (?) it assumes only one notification is active at a time (?)
    // * it has special logic about only showing some notifications once per session which isn't appropriate here.
    [Export(typeof(IUserNotification))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class ConnectUserNotifications : IUserNotification
    {
        private readonly IInfoBarManager infoBarManager;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public ConnectUserNotifications(IInfoBarManager infoBarManager,
            ILogger logger,
            IThreadHandling threadHandling)
        { 
            this.infoBarManager = infoBarManager;
            this.threadHandling = threadHandling;
        }

        public bool HideNotification(Guid id)
        {
            // TODO
            return true;
        }

        public void ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand)
        {
            // TODO
        }

        public void ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand)
        {
            // TODO
        }
    }
}
