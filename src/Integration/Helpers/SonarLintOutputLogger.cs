/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(ILogger))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SonarLintOutputLogger : ILogger
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ISonarLintSettings sonarLintSettings;

        [ImportingConstructor]
        public SonarLintOutputLogger([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ISonarLintSettings sonarLintSettings)
        {
            this.serviceProvider = serviceProvider;
            this.sonarLintSettings = sonarLintSettings;
        }

        public void WriteLine(string message)
        {
            var prefixedMessage = AddPrefixIfVerboseLogging(message);
            VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, prefixedMessage);
        }

        public void WriteLine(string messageFormat, params object[] args)
        {
            var prefixedMessageFormat = AddPrefixIfVerboseLogging(messageFormat);
            VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, prefixedMessageFormat, args);
        }

        public void LogVerbose(string messageFormat, params object[] args)
        {
            if (sonarLintSettings.DaemonLogLevel == DaemonLogLevel.Verbose)
            {
                var text = args.Length == 0 ? messageFormat : string.Format(messageFormat, args);
                WriteLine("[DEBUG] " + text);
            }
        }

        private string AddPrefixIfVerboseLogging(string message)
        {
            if (sonarLintSettings.DaemonLogLevel == DaemonLogLevel.Verbose)
            {
                message = $"[ThreadId {System.Threading.Thread.CurrentThread.ManagedThreadId}] " + message;
            }
            return message;
        }
    }
}
