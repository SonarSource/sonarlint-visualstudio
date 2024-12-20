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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Logging;

namespace SonarLint.VisualStudio.Integration.Helpers;

internal class SonarLintOutputWindowLogWriter(IServiceProvider serviceProvider) : ILogWriter
{
    public void WriteLine(string message) => VsShellUtils.WriteToSonarLintOutputPane(serviceProvider, message);
}

internal class SonarLintSettingsLogVerbosityIndicator(ISonarLintSettings sonarLintSettings) : ILogVerbosityIndicator
{
    public bool IsVerboseEnabled => sonarLintSettings.DaemonLogLevel == DaemonLogLevel.Verbose;
    public bool IsThreadIdEnabled => IsVerboseEnabled;
}

[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarLintOutputLogger(
    ILoggerFactory logFactory,
    [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
    ISonarLintSettings sonarLintSettings)
{
    [Export(typeof(ILogger))]
    public ILogger Instance { get; } =
        logFactory.Create(
            new SonarLintOutputWindowLogWriter(serviceProvider),
            new SonarLintSettingsLogVerbosityIndicator(sonarLintSettings));
}
