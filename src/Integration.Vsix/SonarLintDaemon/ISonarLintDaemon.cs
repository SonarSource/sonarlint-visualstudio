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
using System.Net;
using Sonarlint;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public delegate void DaemonEventHandler(object sender, EventArgs e);

    public interface ISonarLintDaemon : IDisposable
    {
        bool IsInstalled { get; }
        bool IsRunning { get; }

        void Install();
        event DownloadProgressChangedEventHandler DownloadProgressChanged;
        event AsyncCompletedEventHandler DownloadCompleted;

        event EventHandler<EventArgs> Ready;

        void Start();
        void Stop();

        void RequestAnalysis(string path, string charset, string sqLanguage, IIssueConsumer consumer);
    }

    public interface IIssueConsumer
    {
        void Accept(string path, IEnumerable<Issue> issues);
    }
}
