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
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public interface IDaemonInstaller
    {
        bool InstallInProgress { get; }
        bool IsInstalled();
       
        string InstallationPath { get; }
        string DaemonVersion { get; }

        void Install();
        event InstallationProgressChangedEventHandler InstallationProgressChanged;
        event AsyncCompletedEventHandler InstallationCompleted;
    }

    public delegate void InstallationProgressChangedEventHandler(object sender, InstallationProgressChangedEventArgs e);

    // We can't write tests against the WebClient.DownloadProgressChangedEventArgs directly because it doesn't
    // have a public constructor, so we've created our own custom event
    public class InstallationProgressChangedEventArgs : EventArgs
    {
        public InstallationProgressChangedEventArgs(long bytesReceived, long totalBytesToReceive)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
        }

        public long BytesReceived { get; }
        public long TotalBytesToReceive { get; }
    }
}
